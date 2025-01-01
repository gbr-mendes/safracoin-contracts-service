namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface ISmartContractService
{
    Task<bool> DeployContractsOnBlockChain();
    Task<bool> CompileContracts();
}
