using Grpc.Core;
using NewsService.Postgres;
using NewsService.Postgres.Enums;
using PostgresModel = NewsService.Postgres.Models;

namespace NewsService.Api.Services;

public class NewsService(IConfiguration configuration, NewsDbContext dbContext) : News.NewsBase
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
        
        return new SaveNewsResponse();
    }
}