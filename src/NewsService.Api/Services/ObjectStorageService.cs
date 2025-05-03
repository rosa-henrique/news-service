using Grpc.Core;
using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace NewsService.Api.Services;

public class ObjectStorageService(IAmazonS3 client) : ObjectStorage.ObjectStorageBase
{
    public override async Task<GetPreSignedUrlResponse> GetPreSignedUrl(GetPreSignedUrlRequest request,
        ServerCallContext context)
    {
        var objectKey = $"{Guid.NewGuid()}.txt";

        var presignedGetObjectArgs = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            Key = objectKey,
            BucketName = "temporary",
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
        };
        
        var presignedGetObjectAsync = await client.GetPreSignedURLAsync(presignedGetObjectArgs);

        return new GetPreSignedUrlResponse
        {
            Url = presignedGetObjectAsync.Replace("https", "http"),
            Key = objectKey
        };
    }
}