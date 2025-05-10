using MassTransit;
using NewsService.Contracts;
using NewsService.ProcessFile.FilesProcessors;
using NewsService.ProcessFile.Service;

namespace NewsService.ProcessFile.Consumers;

public class ProcessNewsFilesConsumer(IFileProcessingService fileProcessingService) : IConsumer<ProcessNewsFiles>
{
    public async Task Consume(ConsumeContext<ProcessNewsFiles> context)
    {
        if (context.Message.CurrentFile >= context.Message.Files.Count)
            return;
        
        await fileProcessingService.ProcessFile(context.Message);
    }
}