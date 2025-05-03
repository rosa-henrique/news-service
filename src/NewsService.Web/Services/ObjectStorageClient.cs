using NewsService.Api;

namespace NewsService.Web.Services;

public class ObjectStorageClient(ObjectStorage.ObjectStorageClient client)
{
    public async Task<GetPreSignedUrlResponse> GetPreSignedUrl(string contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            ContentType = contentType
        };
        var response = await client.GetPreSignedUrlAsync(request);
        return new GetPreSignedUrlResponse(response.Url, response.Key);
    }
}

public record GetPreSignedUrlResponse(string Url, string Key);