using System.Collections.ObjectModel;
using Grpc.Core;
using MassTransit;
using NewsService.Contracts;
using NewsService.Contracts.Enums;
using NewsService.Postgres;
using NewsService.Postgres.Enums;
using PostgresModel = NewsService.Postgres.Models;

namespace NewsService.Api.Services;

public class NewsService(IConfiguration configuration, NewsDbContext dbContext, IPublishEndpoint publishEndpoint)
    : News.NewsBase
{
    private readonly string _bucketTemporary = configuration.GetValue<string>("BUCKET_TEMPORARY")!;

    public override async Task<SaveNewsResponse> SaveNews(SaveNewsRequest request, ServerCallContext context)
    {
        var createdAt = DateTime.UtcNow;
        var news = new PostgresModel.News
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            Document = new PostgresModel.DocumentFile
            {
                Id = Guid.NewGuid(),
                Bucket = _bucketTemporary,
                CreatedAt = createdAt,
                FilePath = request.Document.ObjectKey,
                Status = StatusFile.Pending,
            },
            Image = new PostgresModel.ImageFile
            {
                Id = Guid.NewGuid(),
                Bucket = _bucketTemporary,
                CreatedAt = createdAt,
                FilePath = request.Image.ObjectKey,
                Status = StatusFile.Pending,
            },
            Video = new PostgresModel.VideoFile
            {
                Id = Guid.NewGuid(),
                Bucket = _bucketTemporary,
                CreatedAt = createdAt,
                FilePath = request.Video.ObjectKey,
                Status = StatusFile.Pending,
            }
        };

        dbContext.News.Add(news);
        await dbContext.SaveChangesAsync();

        await SendToProcess(news);

        return new SaveNewsResponse();
    }

    private async Task SendToProcess(PostgresModel.News news)
    {
        var files = new List<ProcessFiles>
        {
            new (
                FileId: news.Document.Id,
                FileType: ProcessFilesTypes.Document,
                BucketTemporary: _bucketTemporary,
                TemporaryPath: news.Document.FilePath,
                PermanentBucket: null,
                PermanentPath: null,
                Status: StatusProcessingFile.Pending,
                ErrorMessage: null),
            new (
                FileId: news.Image.Id,
                FileType: ProcessFilesTypes.Image,
                BucketTemporary: _bucketTemporary,
                TemporaryPath: news.Image.FilePath,
                PermanentBucket: null,
                PermanentPath: null,
                Status: StatusProcessingFile.Pending,
                ErrorMessage: null),
            new (
                FileId: news.Video.Id,
                FileType: ProcessFilesTypes.Video,
                BucketTemporary: _bucketTemporary,
                TemporaryPath: news.Video.FilePath,
                PermanentBucket: null,
                PermanentPath: null,
                Status: StatusProcessingFile.Pending,
                ErrorMessage: null)
        };
        
        await publishEndpoint.Publish<ProcessNewsFiles>(new(news.Id, files));
    }
}