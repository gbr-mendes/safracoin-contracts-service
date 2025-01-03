namespace SafraCoinContractsService.Core.Interfaces.Repositories;

public interface IRedisRepository
{
    Task<IEnumerable<T>> ReadEntriesFromStreamAsync<T>(
        string streamKey,
        string groupName,
        string consumerName,
        string readType,
        int count,
        Func<byte[], string, T> parseFunction);
    
    Task<bool> CreateGroupAsync(string streamKey, string groupName, string beginPosition);
    Task<bool> AckEntryStreamGroupAsync(string streamKey, string groupName, string entryId);
}
