using SafraCoinContractsService.Core.Interfaces.Repositories;
using StackExchange.Redis;

namespace SafraCoinContractsService.Infra.Repositories;

public class RedisRepository : IRedisRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
    }

    public async Task<IEnumerable<T>> ReadEntriesFromStreamAsync<T>(
        string streamKey,
        string groupName,
        string consumerName,
        string readType,
        int count,
        Func<byte[], string, T> parseFunction)
    {
          var entries = await _database.StreamReadGroupAsync(
            streamKey,
            groupName,
            consumerName,
            readType,
            count: count
        );

        if (!entries.Any())
        {
            return Enumerable.Empty<T>();
        }

        var processedEntries = entries.Select(entry =>
        {
            var buffer = entry.Values.First().Value;
            var redisEntryId = entry.Id;
            var parsedEntry = parseFunction(buffer, redisEntryId);
            return parsedEntry;
        });

        return processedEntries.ToList();
    }

    public async Task<bool> CreateGroupAsync(string streamKey, string groupName, string beginPosition)
    {
        var keyNotExists = !await _database.KeyExistsAsync(streamKey);
        if (keyNotExists)
        {
            await CreateConsumerGroupAsync(streamKey, groupName, beginPosition);
            return true;
        }

        var groupInfo = await _database.StreamGroupInfoAsync(streamKey);
        var groupExists = groupInfo.Any(x => x.Name == groupName);
        if (!groupExists)
        {
            await CreateConsumerGroupAsync(streamKey, groupName, beginPosition);
            return true;
        }

        return false;
    }

    public async Task<bool> AckEntryStreamGroupAsync(string streamKey, string groupName, string entryId)
    {
        var entryAcknowledgeQty = await _database.StreamAcknowledgeAsync(
            streamKey,
            groupName,
            entryId);

        return entryAcknowledgeQty > 0;
    }

    private async Task CreateConsumerGroupAsync(string streamKey, string groupName, string beginPosition)
    {
        var success = await _database.StreamCreateConsumerGroupAsync(streamKey, groupName, beginPosition, createStream: true);
        if (!success)
        {
            throw new Exception("An error has occurred while creating consumer group");
        }
    }
}
