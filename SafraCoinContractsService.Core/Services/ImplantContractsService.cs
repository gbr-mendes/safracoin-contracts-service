using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json.Linq;
using Optional.Unsafe;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.Settings;
using SafraCoinContractsService.Core.ValueObjects;

namespace SafraCoinContractsService.Core.Services
{
    public class ImplantContractsService : IImplantContractsService
    {
        private readonly ISmartContractRepository _smartContractRepository;
        private readonly BlockChainSettings _blockChainSettings;
        private readonly IDockerService _dockerService;
        private readonly ILogger<ImplantContractsService> _logger;
        private readonly string hardHatBaseDir = "../Hardhat";

        public ImplantContractsService(
            ISmartContractRepository smartContractRepository,
            IOptions<BlockChainSettings> blockChainSettings,
            IDockerService dockerService,
            ILogger<ImplantContractsService> logger
            )
        {
            _smartContractRepository = smartContractRepository;
            _blockChainSettings = blockChainSettings.Value;
            _dockerService = dockerService;
            _logger = logger;
        }

        public async Task DeployContractsOnBlockChain()
        {
            await DeployOracleContract();
            await DeploySafraCoinMainContract();
        }

        public async Task CompileContracts()
        {
            var shouldCompile = await ShouldCompile();

            if (!shouldCompile)
            {
                _logger.LogInformation("Skipping contract compilation. No changes detected.");
                return;
            }

            try
            {
                var dockerfileFullPath = Path.Combine(hardHatBaseDir, "Dockerfile");
                if (!File.Exists(dockerfileFullPath))
                {
                    _logger.LogError("Dockerfile not found at: {dockerfileFullPath}", dockerfileFullPath);
                    return;
                }

                var artifactsPath = $"{hardHatBaseDir}/artifacts";
                var containerArtifactsPath = "/app/artifacts";
                var imageName = "hardhat-compiler";

                _logger.LogInformation("Building hardhat docker image. It might take a while...");

                var imageBuilt = await _dockerService.BuildImageAsync(dockerfileFullPath, hardHatBaseDir, imageName);

                if (!imageBuilt)
                {
                    return;
                }

                _logger.LogInformation("Compiling contracts...");

                 var containerStarted = await _dockerService.RunContainerAsync(
                    imageName: imageName,
                    containerName: "hardhat-compiler-container",
                    hostVolumePath: artifactsPath,
                    containerVolumePath: containerArtifactsPath,
                    autoRemove: true
                );

                if (!containerStarted)
                {
                    return;
                }

                _logger.LogInformation("Contracts compiled successfully.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error compiling contracts: {}", ex.Message);
                return;
            }
        }

        private async Task DeployOracleContract()
        {
            var contractName = "SafraCoinOracle";

            await DeploySmartContract(contractName);
        }

        private async Task DeploySafraCoinMainContract()
        {
            var oracleContract = await _smartContractRepository.FindByName("SafraCoinOracle");
            if (!oracleContract.HasValue)
            {
                _logger.LogError("Oracle contract not found. Please deploy the Oracle contract first.");
                return;
            }
            
            var oracleContractAddress = oracleContract.ValueOrFailure().Address;

            var contractName = "SafraCoinTokenGenerator";

            await DeploySmartContract(contractName, new object[] {
                _blockChainSettings.TokenName,
                _blockChainSettings.TokenSymbol,
                oracleContractAddress });
        }

        private async Task DeploySmartContract(string contractName, object[]? contructorParams = null)
        {
            var deployedContract = await _smartContractRepository.FindByName(contractName);
            if (deployedContract.HasValue)
            {
                var contract = deployedContract.ValueOrFailure();
                _logger.LogInformation("{contractName} contract already deployed at address {address}. Skipping", contractName, contract.Address);
                return;
            }
            
            var contractPropertiesFilePath = Path.Combine(
                hardHatBaseDir, 
                "artifacts",
                "contracts",
                $"{contractName}.sol",
                $"{contractName}.json");
            
            if (!File.Exists(contractPropertiesFilePath))
            {
                _logger.LogError("{contractName} contract properties file not found at: {contractPropertiesFilePath}", contractName, contractPropertiesFilePath);
                return;
            }
            
            var contractPropertiesJson = await File.ReadAllTextAsync(contractPropertiesFilePath);
            var contractProperties = JObject.Parse(contractPropertiesJson);
            var abi = contractProperties["abi"]?.ToString();
            var bytecode = contractProperties["bytecode"]?.ToString();

            var account = new Account(_blockChainSettings.PrivateKey);
            var web3 = new Nethereum.Web3.Web3(account, _blockChainSettings.RpcUrl);
            var gasLimit = new Nethereum.Hex.HexTypes.HexBigInteger(_blockChainSettings.GasLimit);

            var deploymentHandler = web3.Eth.DeployContract;
            
            try
            {
                var transactionHash = await deploymentHandler.SendRequestAsync(
                    abi: abi,
                    contractByteCode: bytecode,
                    from: _blockChainSettings.AccountAddress,
                    gas: gasLimit,
                    values: contructorParams ?? Array.Empty<object>()
                );

                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (receipt == null || receipt.ContractAddress == null)
                {
                    _logger.LogError("{contractName} contract deployment failed. Transaction hash: {transactionHash}", contractName, transactionHash);
                    return;
                }

                var rawContractPath = Path.Combine(hardHatBaseDir, "contracts", $"{contractName}.sol");
                var rawCodeHash = ComputeSHA256(rawContractPath);

                var contractAddress = receipt.ContractAddress;

                await _smartContractRepository.Add(new SmartContractVO(
                    contractName,
                    contractAddress,
                    abi,
                    bytecode,
                    rawCodeHash
                ));

                _logger.LogInformation("{contractName} contract deployed successfuly. Address: {contractAddress}", contractName, contractAddress);
                
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{contractName} contract deployment failed.", contractName);
                return;
            }
        }

        private async Task<bool> ShouldCompile()
        {
            var shouldCompile = true;

            foreach(var filePath in Directory.GetFiles(Path.Combine(hardHatBaseDir, "contracts")))
            {
                var fileName = Path.GetFileName(filePath);
                if (!fileName.EndsWith(".sol"))
                {
                    continue;
                }
                var contractName = Path.GetFileNameWithoutExtension(fileName);

                var result = await _smartContractRepository.FindByName(contractName);
                if (!result.HasValue)
                {
                    continue;
                }

                var smartContract = result.ValueOrFailure();

                var rawCodeHash = ComputeSHA256(filePath);

                if (smartContract.RawCodeHash == rawCodeHash)
                {
                    shouldCompile = false;
                    break;
                }
            }
            return shouldCompile;
        }

        private static string ComputeSHA256(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            
            var hashBytes = sha256.ComputeHash(fileStream);
            
            StringBuilder hashString = new();
            foreach (var b in hashBytes)
            {
                hashString.Append(b.ToString("x2"));
            }
            return hashString.ToString();
        }
    }
}
