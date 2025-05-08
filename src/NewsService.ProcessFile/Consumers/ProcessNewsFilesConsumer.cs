using MassTransit;
using NewsService.Contracts;
using NewsService.ProcessFile.FilesProcessors;
using NewsService.ProcessFile.Service;

namespace NewsService.ProcessFile.Consumers;

public class ProcessNewsFilesConsumer(FileProcessingService fileProcessingService) : IConsumer<ProcessNewsFiles>
{
    public async Task Consume(ConsumeContext<ProcessNewsFiles> context)
    {
       await fileProcessingService.ProcessFile(context.Message);
    }
}