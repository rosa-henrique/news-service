using System.Web;

namespace NewsService.Web.Services;

public class ObjectStorageService(IHttpClientFactory clientFactor)
{
    private readonly HttpClient _httpClient = clientFactor.CreateClient();

    public async Task UploadFile(string preSignedUrl, string contentType, Stream fileStream, long contentLength)
    {
        using var content = new StreamContent(fileStream);
        content.Headers.ContentLength = contentLength;
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        var response = await _httpClient.PutAsync(preSignedUrl, content);
        var a = await response.Content.ReadAsStringAsync();
    }
}