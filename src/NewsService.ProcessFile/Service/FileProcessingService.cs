using System.Diagnostics;
using MassTransit;
using NewsService.Contracts;
using NewsService.ProcessFile.FilesProcessors;

namespace NewsService.ProcessFile.Service;

public interface IFileProcessingService
{
    Task ProcessFile(ProcessNewsFiles news);
}

public class FileProcessingService(
    IFileProcessorFactory fileProcessorFactory,
    IPublishEndpoint publishEndpoint,
    ILogger<FileProcessingService> logger) : IFileProcessingService
{
    public async Task ProcessFile(ProcessNewsFiles news)
    {
        if (news.CurrentFile < 0 || news.CurrentFile >= news.Files.Count)
            throw new ArgumentOutOfRangeException("news.CurrentFile");

        var startTime = Stopwatch.GetTimestamp();

        var file = news.Files[news.CurrentFile];
        var folder = news.NewsId.ToString();

        var processor = fileProcessorFactory.GetProcessor(file.FileType);

        var fileSaved = await processor.ProcessFile(file, folder);

        var difference = Stopwatch.GetElapsedTime(startTime);

        logger.LogInformation("Processing file {File} took {Time}", file, difference);

        var newsProcessed = news with
        {
            Files = news.Files.Select(f => f.FileId == file.FileId ? fileSaved : f).ToList(),
            CurrentFile = news.CurrentFile + 1
        };

        await publishEndpoint.Publish(newsProcessed);
    }
}