using Microsoft.EntityFrameworkCore;
using SafraCoinContractsService.Core.Models;

namespace SafraCoinContractsService.Infra.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
        SmartContracts = Set<SmartContract>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }

    public DbSet<SmartContract> SmartContracts { get; set; }
}
