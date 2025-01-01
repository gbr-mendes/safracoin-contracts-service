using Microsoft.EntityFrameworkCore;
using Optional;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Models;
using SafraCoinContractsService.Core.ValueObjects;
using SafraCoinContractsService.Infra.Db;

namespace SafraCoinContractsService.Infra.Repositories.EntiteisRepositories;

public class SmartContractRepository : ISmartContractRepository
{
    private readonly AppDbContext _dbContext;
    
    public SmartContractRepository(AppDbContext context)
    {
        _dbContext = context;
    }

    public async Task<bool> Add(SmartContractVO smartContractVo)
    {
        var smartContract = new SmartContract
        {
            Name = smartContractVo.Name,
            Address = smartContractVo.Address,
            Abi = smartContractVo.Abi,
            ByteCode = smartContractVo.ByteCode,
            RawCodeHash = smartContractVo.RawCodeHash
        };

        await _dbContext.SmartContracts.AddAsync(smartContract);

        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<Option<SmartContractVO>>  FindByName(string name)
    {
        var result = from sc in _dbContext.SmartContracts 
            where sc.Name == name 
            select new SmartContractVO(
                sc.Name,
                sc.Address,
                sc.Abi,
                sc.ByteCode,
                sc.RawCodeHash);
            
        var smartContract = await result.FirstOrDefaultAsync();

        return smartContract != null ? Option.Some(smartContract) : Option.None<SmartContractVO>();
    }
}
