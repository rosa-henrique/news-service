using Grpc.Core;
using NewsService.Api;

namespace NewsService.Api.Services;

public class ObjectStorageService : ObjectStorage.ObjectStorageBase
{
    public override async Task<GetPreSignedUrlResponse> GetPreSignedUrl(GetPreSignedUrlRequest request, ServerCallContext context)
    {
        return new GetPreSignedUrlResponse
        {
            Url = "",
            Key = "url de sl"
        };
    }
}