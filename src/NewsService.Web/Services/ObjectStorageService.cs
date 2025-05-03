using System.Web;

namespace NewsService.Web.Services;

public class ObjectStorageService(IHttpClientFactory clientFactor)
{
    private readonly HttpClient _httpClient = clientFactor.CreateClient();

    public async Task UploadFile(string preSignedUrl, string fileName, Stream fileStream, long contentLength)
    {
        var a = preSignedUrl;
        var b = HttpUtility.UrlDecode(preSignedUrl);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentLength = contentLength;
        await _httpClient.PutAsync( b, content);
    }
}