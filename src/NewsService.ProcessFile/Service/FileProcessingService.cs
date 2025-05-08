using MassTransit;
using NewsService.Contracts;
using NewsService.ProcessFile.FilesProcessors;

namespace NewsService.ProcessFile.Service;

public class FileProcessingService(IFileProcessorFactory fileProcessorFactory, IPublishEndpoint publishEndpoint)
{
    public async Task ProcessFile(ProcessNewsFiles news)
    {
        if (news.CurrentFile < 0 || news.CurrentFile >= news.Files.Count)
            throw new ArgumentOutOfRangeException("news.CurrentFile");

        var file = news.Files[news.CurrentFile];
        var folder = news.NewsId.ToString();
        var lastFile = news.CurrentFile == news.Files.Count - 1;

        var processor = fileProcessorFactory.GetProcessor(file.FileType);

        var fileSaved = await processor.ProcessFile(file, folder);

        var newsProcessed = news with
        {
            Files = news.Files.Select(f => f.FileId == file.FileId ? fileSaved : f).ToList(),
            CurrentFile = lastFile ? news.CurrentFile : news.CurrentFile + 1
        };

        if (lastFile)
            return;

        await publishEndpoint.Publish(newsProcessed);
    }
}