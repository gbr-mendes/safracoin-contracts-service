using System.Diagnostics;
using SafraCoinContractsService.Core.Interfaces;

namespace SafraCoinContractsService.Core.Services
{
    public class ImplementContractService : IImplementContractService
    {
        private readonly string baseDirectory = "../Hardhat";

        public async Task<bool> BuildContract()
        {
            try
            {
                var dockerfileFullPath = Path.Combine(baseDirectory, "Dockerfile");
                if (!File.Exists(dockerfileFullPath))
                {
                    Console.WriteLine($"Dockerfile not found at: {dockerfileFullPath}");
                    return false;
                }

                var artifactsPath = $"{baseDirectory}/artifacts";
                var containerArtifactsPath = "/app/artifacts";
                var imageName = "hardhat-compiler";

                Console.WriteLine("Building hardhat docker image. It might take a while...");

                // Criando o comando para build da imagem
                var buildCommand = $"build -t {imageName} {baseDirectory}";

                var buildProcess = RunDockerCommand(buildCommand);

                // Lendo a saída enquanto o processo está em execução
                buildProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                buildProcess.BeginOutputReadLine();
                buildProcess.BeginErrorReadLine();

                await buildProcess.WaitForExitAsync();

                if (buildProcess.ExitCode != 0)
                {
                    Console.WriteLine("Error building Docker image.");
                    return false;
                }

                Console.WriteLine("Hardhat docker image built successfully.");
                Console.WriteLine("Compiling contracts...");

                var runCommand = $"run --rm -v {Path.GetFullPath(artifactsPath)}:{containerArtifactsPath} {imageName}";

                var runProcess = RunDockerCommand(runCommand);

                runProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                runProcess.BeginOutputReadLine();
                runProcess.BeginErrorReadLine();

                await runProcess.WaitForExitAsync();

                if (runProcess.ExitCode != 0)
                {
                    Console.WriteLine("Error compiling contracts.");
                    return false;
                }

                Console.WriteLine("Contracts compiled successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao compilar contratos: {ex.Message}");
                return false;
            }
        }

        private static Process RunDockerCommand(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo) ?? throw new Exception("Failed to start Docker process.");
            return process;
        }
    }
}
