using Grpc.Core;
using System;
using System.Collections.Frozen;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace NewsService.Api.Services;

public class ObjectStorageService(IAmazonS3 client) : ObjectStorage.ObjectStorageBase
{
    private static IDictionary<string, string> contentTypesToExtensions = new Dictionary<string, string>
    {
        { "application/pdf", "pdf" }
    }.ToFrozenDictionary();

    public override async Task<GetPreSignedUrlResponse> GetPreSignedUrl(GetPreSignedUrlRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ContentType))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content Type is required."));
        }

        if (!contentTypesToExtensions.TryGetValue(request.ContentType, out var extension))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content Type is invalid."));
        }

        var objectKey = $"{Guid.NewGuid()}.{extension}";

        var presignedGetObjectArgs = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            Key = objectKey,
            BucketName = "temporary",
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = request.ContentType
        };

        var presignedGetObjectAsync = await client.GetPreSignedURLAsync(presignedGetObjectArgs);

        return new GetPreSignedUrlResponse
        {
            Url = presignedGetObjectAsync.Replace("https", "http"),
            Key = objectKey
        };
    }
}