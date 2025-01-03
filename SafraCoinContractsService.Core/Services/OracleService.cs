using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using Optional.Unsafe;
using SafraCoinContractsService.Core.Interfaces.Repositories.EFRepository;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.Settings;

namespace SafraCoinContractsService.Core.Services;

public class OracleService : IOracleService
{
    private readonly ISmartContractRepository _smartContractRepository;
    private readonly BlockChainSettings _blockChainSettings;
    private readonly ILogger<OracleService> _logger;
    private static readonly string HardhatBaseDir = "../Hardhat";
    private static readonly string ContractName = "SafraCoinOracle";

    public OracleService(ISmartContractRepository smartContractRepository,IOptions<BlockChainSettings> blockChainSettings, ILogger<OracleService> logger)
    {
        _smartContractRepository = smartContractRepository;
        _blockChainSettings = blockChainSettings.Value;
        _logger = logger;
    }

    public async Task SetAuthorization(string accountAddress, bool isAuthorized = true)
    {
        var web3 = new Web3(_blockChainSettings.RpcUrl);

        // TODO: Abstract the read json properties logic to a sdk method so it can be reused across the application

        var contractPropertiesFilePath = Path.Combine(
                HardhatBaseDir, 
                "artifacts",
                "contracts",
                $"{ContractName}.sol",
                $"{ContractName}.json");
            
        if (!File.Exists(contractPropertiesFilePath))
        {
            _logger.LogError("[{serviceName}] {contractName} contract properties file not found at: {contractPropertiesFilePath}", nameof(OracleService), ContractName, contractPropertiesFilePath);
            return;
        }
        
        var contractPropertiesJson = await File.ReadAllTextAsync(contractPropertiesFilePath);
        var contractProperties = JObject.Parse(contractPropertiesJson);
        var abi = contractProperties["abi"]?.ToString();

        var contractAddress = await GetContractAddress(ContractName);

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var setAuthorizationFunction = contract.GetFunction("setAuthorization");

        var receipt = await setAuthorizationFunction.SendTransactionAndWaitForReceiptAsync(
            accountAddress,
            new HexBigInteger(_blockChainSettings.GasLimit),
            null,
            null,
            accountAddress,
            isAuthorized
        );
        
        if (!receipt.Succeeded())
        {
            _logger.LogError("[{serviceName}] Transaction failed. Error setting authorization for address {accountAddress}", nameof(OracleService), accountAddress);
            return;
        }

        _logger.LogInformation("[{serviceName}] Transaction succeeded. Authorization set for address {accountAddress}", nameof(OracleService), accountAddress);
    }

    private async Task<string> GetContractAddress(string contractName)
    {
        var smartContract = await _smartContractRepository.FindByName(contractName);
        if (!smartContract.HasValue)
        {
            throw new Exception($"Smart contract {contractName} not found");
        }

        return smartContract.ValueOrFailure().Address;
    }
}
