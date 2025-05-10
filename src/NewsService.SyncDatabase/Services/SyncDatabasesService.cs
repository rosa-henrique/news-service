using System.Collections.Concurrent;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using NewsService.Contracts;
using NewsService.Contracts.Enums;
using NewsService.Postgres;
using NewsService.Postgres.Enums;
using NewsService.SyncDatabase.Models;

namespace NewsService.SyncDatabase.Services;

public interface ISyncDatabasesService
{
    Task ProcessMessage(ProcessNewsFiles news);
}

public class SyncDatabasesService(ElasticsearchClient elasticClient, NewsDbContext dbContext) : ISyncDatabasesService
{
    //private static ConcurrentDictionary<Guid, object> _lockObjects = new ConcurrentDictionary<Guid, object>();
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _semaphores = new();

    public async Task ProcessMessage(ProcessNewsFiles news)
    {
        // var lockObject = _lockObjects.GetOrAdd(news.NewsId, new object());
        //
        // lock (lockObject)
        // {
        //  try {}
        //  finally
        //  {
        //     // Remove o lock após uso (opcional, dependendo do cenário)
        //     _lockObjects.TryRemove(news.NewsId, out _);
        //  }
        //}

        var semaphore = _semaphores.GetOrAdd(news.NewsId, new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            var tasks = new List<Task>();
            if (news.CurrentFile == 0)
            {
                tasks.Add(SaveNewsState(news));
                tasks.Add(SaveAudit(news.NewsId, news.Files.First()));
            }
            else if (news.CurrentFile == news.Files.Count)
            {
                tasks.Add(UpdateNewsState(news));
                tasks.Add(SaveAudit(news.NewsId, news.Files.Last()));
                tasks.Add(UpdateStatusFiles(news.Files));
            }
            else
            {
                tasks.Add(SaveAudit(news.NewsId, news.Files[news.CurrentFile]));
                tasks.Add(SaveAudit(news.NewsId, news.Files[news.CurrentFile - 1]));
            }

            if (tasks.Count != 0)
                await Task.WhenAll(tasks);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task SaveNewsState(ProcessNewsFiles news)
    {
        var newsState = new NewsState
        {
            Id = news.NewsId.ToString(),
            TypeProcess = NewsStateTypeProcess.Created,
            Status = NewsStateStatus.ProcessingInit,
            Files = news.Files.Select(f => new NewsStateFile
            {
                Status = NewsStateFileStatus.Success,
                FileId = f.FileId.ToString()
            }).ToList()
        };

        var response = await elasticClient.IndexAsync(newsState, idx => idx.Index("news_state"));

        if (!response.IsValidResponse)
            throw new InvalidOperationException("Save NewsState with errors");
    }

    private async Task UpdateNewsState(ProcessNewsFiles news)
    {
        var id = news.NewsId.ToString();
        var response = await elasticClient.GetAsync<NewsState>(id, idx => idx.Index("news_state"));

        if (!response.IsValidResponse)
            throw new InvalidOperationException("Save NewsState with errors");

        var newsState = response.Source!;

        newsState.Status = NewsStateStatus.Success;
        newsState.Files = newsState.Files.Select(f =>
        {
            var file = news.Files.FirstOrDefault(nf => nf.FileId.ToString() == f.FileId)!;

            f.Status = file.Status == StatusProcessingFile.Failed
                ? NewsStateFileStatus.Failed
                : NewsStateFileStatus.Success;
            return f;
        }).ToList();

        var responseUpdate = await elasticClient.UpdateAsync<NewsState, NewsState>("news_state", id, u => u
            .Doc(newsState));

        if (!responseUpdate.IsValidResponse)
            throw new InvalidOperationException("Update NewsState with errors");
    }

    private async Task SaveAudit(Guid newsId, ProcessFiles file)
    {
        var audit = new NewsStateAudit
        {
            Id = Guid.NewGuid().ToString(),
            NewsId = newsId.ToString(),
            FileId = file.FileId.ToString(),
            Status = file.Status switch
            {
                StatusProcessingFile.Failed => NewsStateStatus.Failed,
                StatusProcessingFile.Completed => NewsStateStatus.Success,
                _ => NewsStateStatus.ProcessingInit
            }
        };

        var response = await elasticClient.IndexAsync(audit, idx => idx.Index("news_state_audit"));

        if (!response.IsValidResponse)
            throw new InvalidOperationException("Save NewsStateAudit with errors");
    }

    private async Task UpdateStatusFiles(IReadOnlyList<ProcessFiles> files)
    {
        foreach (var file in files)
        {
            var newStatus = file.Status switch
            {
                StatusProcessingFile.Failed => StatusFile.Failed,
                _ => StatusFile.Completed
            };

            var permanentBucket = file.Status == StatusProcessingFile.Completed ? file.PermanentBucket : null;
            var permanentPathFile = file.Status == StatusProcessingFile.Completed ? file.PermanentPath : null;

            switch (file.FileType)
            {
                case ProcessFilesTypes.Document:
                    await dbContext.NewsDocumentsFiles.Where(nf => nf.Id == file.FileId)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.ErrorMessage, file.ErrorMessage)
                            .SetProperty(p => p.ProcessedAt, DateTime.UtcNow)
                            .SetProperty(p => p.Status, newStatus)
                            .SetProperty(p => p.Bucket, permanentBucket)
                            .SetProperty(p => p.FilePath, permanentPathFile));
                    break;
                case ProcessFilesTypes.Image:
                    await dbContext.NewsImagesFiles.Where(nf => nf.Id == file.FileId)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.ErrorMessage, file.ErrorMessage)
                            .SetProperty(p => p.ProcessedAt, DateTime.UtcNow)
                            .SetProperty(p => p.Status, newStatus)
                            .SetProperty(p => p.Bucket, permanentBucket)
                            .SetProperty(p => p.FilePath, permanentPathFile));
                    break;
                case ProcessFilesTypes.Video:
                    await dbContext.NewsVideosFiles.Where(nf => nf.Id == file.FileId)
                        .ExecuteUpdateAsync(u => u.SetProperty(p => p.ErrorMessage, file.ErrorMessage)
                            .SetProperty(p => p.ProcessedAt, DateTime.UtcNow)
                            .SetProperty(p => p.Status, newStatus)
                            .SetProperty(p => p.Bucket, permanentBucket)
                            .SetProperty(p => p.FilePath, permanentPathFile));
                    break;
            }
        }
    }
    
    
}