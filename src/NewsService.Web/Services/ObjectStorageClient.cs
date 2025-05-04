using NewsService.Api;

namespace NewsService.Web.Services;

public class ObjectStorageClient(ObjectStorage.ObjectStorageClient client)
{
    public async Task<GetPreSignedUrlResponse> GetPreSignedUrl(string contentType, string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            ContentType = contentType,
            FileName = fileName
        };
        var response = await client.GetPreSignedUrlAsync(request);
        return new GetPreSignedUrlResponse(response.PreSignedUrl, response.ObjectKey);
    }

    public async Task<GetPreSignedUrlMultiPartResponse> GetPreSignedUrlMultiPart(string contentType, string fileName,
        long fileSize)
    {
        var request = new GetPreSignedUrlMultiPartRequest
        {
            ContentType = contentType,
            FileName = fileName,
            FileSize = fileSize
        };
        return await client.GetPreSignedUrlMultiPartAsync(request);
    }

    public async Task CompleteUploadMultiPart(string uploadId, string objectKey, IDictionary<int, string> partUrls)
    {
        var request = new CompleteUploadMultiPartRequest
        {
            UploadId = uploadId,
            ObjectKey = objectKey,
            Etags = { partUrls }
        };

        await client.CompleteUploadMultiPartAsync(request);
    }
}

public record GetPreSignedUrlResponse(string Url, string Key);