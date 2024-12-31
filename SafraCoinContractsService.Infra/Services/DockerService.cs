using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SafraCoinContractsService.Core.Interfaces.Services;
namespace SafraCoinContractsService.Infra.Services;

public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;
    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> BuildImageAsync(string dockerfilePath, string contextPath, string tag, bool noCache = false, bool removeIntermediateContainers = true)
    {
        if (string.IsNullOrWhiteSpace(dockerfilePath))
        {
            throw new ArgumentException("Dockerfile path must not be null or empty.", nameof(dockerfilePath));
        }

        if (string.IsNullOrWhiteSpace(contextPath))
        {
            throw new ArgumentException("Context path must not be null or empty.", nameof(contextPath));
        }

        var command = $"build -f \"{dockerfilePath}\" \"{contextPath}\"";

        if (!string.IsNullOrWhiteSpace(tag))
        {
            command += $" -t \"{tag}\"";
        }

        if (noCache)
        {
            command += " --no-cache";
        }

        if (!removeIntermediateContainers)
        {
            command += " --rm=false";
        }

        try
        {
            var process = RunRawCommand(command);

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Error building image {tag} with command {command}.",tag, command);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while building the Docker image: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RunContainerAsync(
        string imageName, 
        string containerName, 
        string hostVolumePath, 
        string containerVolumePath, 
        bool autoRemove = true, 
        string additionalArgs = "")
    {
        if (string.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Image name must not be null or empty.", nameof(imageName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name must not be null or empty.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(hostVolumePath))
        {
            throw new ArgumentException("Host volume path must not be null or empty.", nameof(hostVolumePath));
        }

        if (string.IsNullOrWhiteSpace(containerVolumePath))
        {
            throw new ArgumentException("Container volume path must not be null or empty.", nameof(containerVolumePath));
        }

        var command = "run";

        if (autoRemove)
        {
            command += " --rm";
        }

        command += $" -v \"{Path.GetFullPath(hostVolumePath)}:{containerVolumePath}\" --name \"{containerName}\" {imageName}";

        if (!string.IsNullOrWhiteSpace(additionalArgs))
        {
            command += $" {additionalArgs}";
        }

        try
        {
            var process = RunRawCommand(command);

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Error running container {containerName} from image {imageName} with command {command}.", containerName, imageName, command);
                return false;
            }

            _logger.LogInformation("Container {containerName} started successfully.", containerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while starting the container {containerName}.", containerName);
            return false;
        }
    }

    public Process RunRawCommand(string command)
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
