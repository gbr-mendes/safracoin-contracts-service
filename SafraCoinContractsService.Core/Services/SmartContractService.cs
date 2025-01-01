using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optional.Unsafe;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.Settings;

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

        public async Task<bool> DeployContractsOnBlockChain()
        {
            await Task.Delay(1000);
            return true;
        }

        public async Task<bool> CompileContracts()
        {
            var shouldCompile = await ShouldCompile();

            if (!shouldCompile)
            {
                _logger.LogInformation("Skipping contract compilation. No changes detected.");
                return false;
            }

            try
            {
                var dockerfileFullPath = Path.Combine(hardHatBaseDir, "Dockerfile");
                if (!File.Exists(dockerfileFullPath))
                {
                    _logger.LogError("Dockerfile not found at: {dockerfileFullPath}", dockerfileFullPath);
                    return false;
                }

                var artifactsPath = $"{hardHatBaseDir}/artifacts";
                var containerArtifactsPath = "/app/artifacts";
                var imageName = "hardhat-compiler";

                _logger.LogInformation("Building hardhat docker image. It might take a while...");

                var imageBuilt = await _dockerService.BuildImageAsync(dockerfileFullPath, hardHatBaseDir, imageName);

                if (!imageBuilt)
                {
                    return false;
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
                    return false;
                }

                _logger.LogInformation("Contracts compiled successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error compiling contracts: {}", ex.Message);
                return false;
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
