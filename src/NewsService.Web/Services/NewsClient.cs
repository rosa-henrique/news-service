using NewsService.Api;
using NewsService.Web.Models;

namespace NewsService.Web.Services;

public class NewsClient(News.NewsClient client)
{
    public async Task SaveNews(NewsFormModel model)
    {
        var request = new SaveNewsRequest
        {
            Title = model.Title,
            Body = model.Body,
            Document = new SaveNewsFileRequest
            {
                ObjectKey = model.Document.ObjectKey,
                FileName = model.Document.FileName
            },
            Image = new SaveNewsFileRequest
            {
                ObjectKey = model.Image.ObjectKey,
                FileName = model.Image.FileName
            },
            Video = new SaveNewsFileRequest
            {
                ObjectKey = model.Video.ObjectKey,
                FileName = model.Video.FileName
            }
        };

        await client.SaveNewsAsync(request);
    }
}