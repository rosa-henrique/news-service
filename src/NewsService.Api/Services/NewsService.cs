using Grpc.Core;

namespace NewsService.Api.Services;

public class NewsService : News.NewsBase
{
    public override async Task<SaveNewsResponse> SaveNews(SaveNewsRequest request, ServerCallContext context)
    {
        return new SaveNewsResponse();
    }
}