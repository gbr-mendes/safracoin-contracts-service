using Microsoft.Extensions.Logging;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.ValueObjects;

namespace SafraCoinContractsService.Core.Services;

public class RedisService : IRedisService
{
    private readonly ILogger<RedisService> _logger;
    private readonly IRedisRepository _redisRepository;

    public RedisService(ILogger<RedisService> logger, IRedisRepository redisRepository)
    {
        _logger = logger;
        _redisRepository = redisRepository;
    }

    public async Task AckEntryStreamGroupAsync(string streamName, string groupName, string entryId)
    {
        await _redisRepository.AckEntryStreamGroupAsync(streamName, groupName, entryId);
    }

    public async Task CreateConsumerGroupIfNotExists(string streamKey, string groupName, string beginPosition)
    {
        var groupCreatedSuccessfully = await _redisRepository.CreateGroupAsync(
            streamKey,
            groupName,
            beginPosition);

        var status = groupCreatedSuccessfully ? "Success" : "AlreadyExists";

        _logger.LogInformation("[{serviceName}] Consumer group {groupName} created: Status: {status}",
            nameof(RedisService),
            groupName,
            status
        );
    }

    public async Task<IEnumerable<RedisEntryVO<T>>> ReadEntriesFromRedisStream<T>(
    string streamName,
    string consumerGroup,
    string consumerName,
    string queryMode, 
    int count, 
    IRedisService.MessageParser<T> parser)
    where T : Google.Protobuf.IMessage
    {
        var entries = await _redisRepository.ReadEntriesFromStreamAsync(
            streamName,
            consumerGroup,
            consumerName,
            queryMode,
            count,
            (buffer, redisEntryId) => {
                var entry = parser(buffer);
                var redisEntryVO = new RedisEntryVO<T>
                {
                    Key = redisEntryId,
                    Value = entry
                };
                return redisEntryVO;
            }
        );

        return entries;
    }
}
