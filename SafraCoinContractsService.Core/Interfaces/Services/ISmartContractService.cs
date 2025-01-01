namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface ISmartContractService
{
    Task DeployContractsOnBlockChain();
    Task CompileContracts();
}
