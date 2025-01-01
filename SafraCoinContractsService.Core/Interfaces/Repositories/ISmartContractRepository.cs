using Optional;
using SafraCoinContractsService.Core.ValueObjects;

namespace SafraCoinContractsService.Core.Interfaces.Repositories;

public interface ISmartContractRepository
{
    Task<bool> Add(SmartContractVO smartContractVo);
    Task<Option<SmartContractVO>> FindByName(string name);
}
