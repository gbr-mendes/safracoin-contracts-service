using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.ValueObjects;

namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface IRedisService
{
    Task CreateConsumerGroupIfNotExists(
        string streamKey,
        string groupName,
        string beginPosition);

    delegate T MessageParser<T>(byte[] buffer) where T : Google.Protobuf.IMessage;

    Task<IEnumerable<RedisEntryVO<T>>> ReadEntriesFromRedisStream<T>(
        string streamName,
        string consumerGroup,
        string consumerName,
        string queryMode, 
        int count, 
        MessageParser<T> parser) 
        where T : Google.Protobuf.IMessage;
    
    Task AckEntryStreamGroupAsync(string streamName, string groupName, string entryId);
}
