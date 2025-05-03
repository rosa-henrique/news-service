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
        var objectKey = $"{Guid.NewGuid()}.txt";
        // var getPreSignedUrlRequest = new Amazon.S3.Model.GetPreSignedUrlRequest()
        // {
        //     BucketName = "temporary",
        //     Key = objectKey,
        //     Expires = DateTime.UtcNow.AddMinutes(15),
        //     Verb = HttpVerb.GET,
        // };
        var presignedGetObjectArgs = new PresignedPutObjectArgs()
                                                .WithBucket("temporary")
                                                .WithObject(objectKey)
                                                .WithExpiry(604800) ;
        var presignedGetObjectAsync = await minioClient.PresignedPutObjectAsync(presignedGetObjectArgs);

        return new GetPreSignedUrlResponse
        {
            Url = presignedGetObjectAsync,
            Key = objectKey
        };
    }
}