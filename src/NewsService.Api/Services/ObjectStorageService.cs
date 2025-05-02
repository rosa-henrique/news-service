using Grpc.Core;
using Minio;
using Minio.DataModel.Args;
using NewsService.Api;

namespace NewsService.Api.Services;

public class ObjectStorageService(IMinioClient minioClient) : ObjectStorage.ObjectStorageBase
{
    public override async Task<GetPreSignedUrlResponse> GetPreSignedUrl(GetPreSignedUrlRequest request,
        ServerCallContext context)
    {
        var key = $"{Guid.NewGuid()}.txt";
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket("temporary")
            .WithObject(key)
            .WithExpiry(10 * 60);
        
        var presignedGetObjectAsync = await minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
        
        return new GetPreSignedUrlResponse
        {
            Url = presignedGetObjectAsync,
            Key = key
        };
    }
}