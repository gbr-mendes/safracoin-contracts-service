using Optional;
using SafraCoinContractsService.Core.ValueObjects;

namespace SafraCoinContractsService.Core.Interfaces.Repositories;

public interface ISmartContractRepository
{
    Task<Option<SmartContractVO>> FindByName(string name);
}
