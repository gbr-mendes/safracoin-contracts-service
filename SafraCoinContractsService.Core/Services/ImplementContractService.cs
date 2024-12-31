using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SafraCoinContractsService.Core.Interfaces.Services;

namespace SafraCoinContractsService.Core.Services
{
    public class ImplementContractService : IImplementContractService
    {
        private readonly ILogger<ImplementContractService> _logger;
        private readonly IDockerService _dockerService;
        private readonly string hardHatBaseDir = "../Hardhat";

        public ImplementContractService(IDockerService dockerService, ILogger<ImplementContractService> logger)
        {
            _dockerService = dockerService;
            _logger = logger;
        }

        public async Task<bool> CompileContracts()
        {
            try
            {
                var dockerfileFullPath = Path.Combine(hardHatBaseDir, "Dockerfile");
                if (!File.Exists(dockerfileFullPath))
                {
                    Console.WriteLine($"Dockerfile not found at: {dockerfileFullPath}");
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
                Console.WriteLine($"Erro ao compilar contratos: {ex.Message}");
                return false;
            }
        }
    }
}
