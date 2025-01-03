namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface IImplantContractsService
{
    Task DeployContractsOnBlockChain();
    Task CompileContracts();
}
