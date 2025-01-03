namespace SafraCoinContractsService.Core.ValueObjects;

public class AccountVO
{
    public required string RedisEntryId { get; set; }
    public required string Address { get; set; }
    public required string Email { get; set; }
}
