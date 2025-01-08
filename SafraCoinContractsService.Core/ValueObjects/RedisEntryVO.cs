namespace SafraCoinContractsService.Core.ValueObjects;

public class RedisEntryVO<T>
{
    public required string Key { get; set; }
    public required T Value { get; set; }
}
