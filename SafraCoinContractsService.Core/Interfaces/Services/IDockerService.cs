using System.Diagnostics;

namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface IDockerService
{
    Task<bool> BuildImageAsync(string dockerfilePath, string contextPath, string tag, bool noCache = false, bool removeIntermediateContainers = true);
    Task<bool> RunContainerAsync(
        string imageName, 
        string containerName, 
        string hostVolumePath, 
        string containerVolumePath, 
        bool autoRemove = true, 
        string additionalArgs = "");
    Process RunRawCommand(string command);
}
