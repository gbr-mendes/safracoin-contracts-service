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
    public class SmartContractService : ISmartContractService
    {
        private readonly ISmartContractRepository _smartContractRepository;
        private readonly BlockChainSettings _blockChainSettings;
        private readonly IDockerService _dockerService;
        private readonly ILogger<SmartContractService> _logger;
        private readonly string hardHatBaseDir = "../Hardhat";

        public SmartContractService(
            ISmartContractRepository smartContractRepository,
            IOptions<BlockChainSettings> blockChainSettings,
            IDockerService dockerService,
            ILogger<SmartContractService> logger
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
            var oracleContractDeployed = await _smartContractRepository.FindByName("SafraCoinOracle");
            if (oracleContractDeployed.HasValue)
            {
                var contract = oracleContractDeployed.ValueOrFailure();
                _logger.LogInformation("Oracle contract already deployed at address {address}. Skipping", contract.Address);
                return;
            }

            var oracleContractName = "SafraCoinOracle";
            var oraclePropertiesFilePath = Path.Combine(
                hardHatBaseDir, 
                "artifacts",
                "contracts",
                $"{oracleContractName}.sol",
                $"TokenAuthorizationOracle.json");
            
            if (!File.Exists(oraclePropertiesFilePath))
            {
                _logger.LogError("Oracle contract properties file not found at: {oraclePropertiesFilePath}", oraclePropertiesFilePath);
                return;
            }
            
            var oraclePropertiesJson = await File.ReadAllTextAsync(oraclePropertiesFilePath);
            var oracleProperties = JObject.Parse(oraclePropertiesJson);
            var oracleAbi = oracleProperties["abi"]?.ToString();
            var oracleByteCode = oracleProperties["bytecode"]?.ToString();

            var account = new Account(_blockChainSettings.PrivateKey);
            var web3 = new Nethereum.Web3.Web3(account, _blockChainSettings.RpcUrl);
            var gasLimit = new Nethereum.Hex.HexTypes.HexBigInteger(_blockChainSettings.GasLimit);

            var deploymentHandler = web3.Eth.DeployContract;
            
            var transactionHash = await deploymentHandler.SendRequestAsync(
                abi: oracleAbi,
                contractByteCode: oracleByteCode,
                from: _blockChainSettings.AccountAddress,
                gas: gasLimit
            );
            
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            if (receipt == null || receipt.ContractAddress == null)
            {
                _logger.LogError("Oracle contract deployment failed. Transaction hash: {transactionHash}", transactionHash);
                return;
            }

            var oracleRawContractPath = Path.Combine(hardHatBaseDir, "contracts", $"{oracleContractName}.sol");
            var contractAddress = receipt.ContractAddress;
            var rawCodeHash = ComputeSHA256(oracleRawContractPath);

            await _smartContractRepository.Add(new SmartContractVO(
                oracleContractName,
                contractAddress,
                oracleAbi,
                oracleByteCode,
                rawCodeHash
            ));

            _logger.LogInformation("Oracle contract deployed successfuly. Address: {contractAddress}", contractAddress);
            
            return;
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
