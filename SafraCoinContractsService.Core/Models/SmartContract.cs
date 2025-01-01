namespace SafraCoinContractsService.Core.Models;

public class SmartContract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string Abi { get; set; }
    public required string ByteCode { get; set; }
    public required string RawCodeHash {get; set; }
}
