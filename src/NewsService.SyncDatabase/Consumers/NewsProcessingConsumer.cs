using MassTransit;
using NewsService.Contracts;
using NewsService.SyncDatabase.Services;

namespace NewsService.SyncDatabase.Consumers;

public class NewsProcessingConsumer(ISyncDatabasesService service) : IConsumer<ProcessNewsFiles>
{
    public async Task Consume(ConsumeContext<ProcessNewsFiles> context)
    {
        await service.ProcessMessage(context.Message);
    }
}