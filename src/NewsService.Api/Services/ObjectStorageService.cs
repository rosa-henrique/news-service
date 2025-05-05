using Grpc.Core;
using System;
using System.Collections.Frozen;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace NewsService.Api.Services;

public class ObjectStorageService(IAmazonS3 client, IConfiguration configuration, ILogger<ObjectStorageService> logger)
    : ObjectStorage.ObjectStorageBase
{
    private readonly string _bucketTemporary = configuration.GetValue<string>("BUCKET_TEMPORARY")!;
    private const string MetadataKey = "original-filename";

    private static IDictionary<string, string> _contentTypesToExtensions = new Dictionary<string, string>
    {
        { "application/pdf", "pdf" },
        { "video/mp4", "mp4" },
        { "image/png", "png" },
        { "image/jpg", "jpg" },
        { "image/jpeg", "jpeg" }
    }.ToFrozenDictionary();

    public override async Task<GetPreSignedUrlResponse> GetPreSignedUrl(GetPreSignedUrlRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.ContentType))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content Type is required."));
        }

        if (string.IsNullOrEmpty(request.FileName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "File Name is required."));
        }

        if (!_contentTypesToExtensions.TryGetValue(request.ContentType, out var extension))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content Type is invalid."));
        }

        var objectKey = $"{Guid.NewGuid()}.{extension}";

        var presignedGetObjectArgs = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            Key = objectKey,
            BucketName = _bucketTemporary,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = request.ContentType,
            Protocol = Protocol.HTTP,
            Headers =
            {
                [$"x-amz-meta-{MetadataKey}"] = request.FileName // Metadado formatado corretamente
            }
        };

        var presignedGetObjectAsync = await client.GetPreSignedURLAsync(presignedGetObjectArgs);

        return new GetPreSignedUrlResponse
        {
            PreSignedUrl = presignedGetObjectAsync,
            ObjectKey = objectKey
        };
    }

    public override async Task<GetPreSignedUrlMultiPartResponse> GetPreSignedUrlMultiPart(
        GetPreSignedUrlMultiPartRequest request,
        ServerCallContext context)
    {
        if (!_contentTypesToExtensions.TryGetValue(request.ContentType, out var extension))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content Type is invalid."));
        }

        var objectKey = $"{Guid.NewGuid()}.{extension}";
        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = _bucketTemporary,
            Key = objectKey,
            ContentType = request.ContentType,
            Metadata = { [MetadataKey] = request.FileName }
        };

        var initResponse = await client.InitiateMultipartUploadAsync(initiateRequest);

        var (partSize, partCount) = CalculateParts(request.FileSize);

        var partUrls = new List<PartUrls>();
        for (var partNumber = 1; partNumber <= partCount; partNumber++)
        {
            var partSizeAtual = partSize;
            if (partNumber == partCount)
                partSizeAtual = request.FileSize - (partSize * (partCount - 1));

            partUrls.Add(new PartUrls
            {
                PartNumber = partNumber,
                PreSignedUrl = GeneratePresignedUrl(objectKey, initResponse.UploadId, partNumber, request.FileName,
                    request.ContentType),
                PartSize = partSizeAtual
            });
        }

        return new GetPreSignedUrlMultiPartResponse()
        {
            UploadId = initResponse.UploadId,
            ObjectKey = objectKey,
            PartSize = partSize,
            TotalParts = partCount,
            PartUrls = { partUrls }
        };
    }

    public override async Task<CompleteUploadMultiPartResponse> CompleteUploadMultiPart(
        CompleteUploadMultiPartRequest request,
        ServerCallContext context)
    {
        var partETags = request.Etags
            .OrderBy(e => e.Key)
            .Select(e => new PartETag(e.Key, e.Value))
            .ToList();

        var completeMultipartUploadRequest = new CompleteMultipartUploadRequest
        {
            BucketName = _bucketTemporary,
            Key = request.ObjectKey,
            UploadId = request.UploadId,
            PartETags = partETags
        };

        var a = await client.CompleteMultipartUploadAsync(completeMultipartUploadRequest);

        return new CompleteUploadMultiPartResponse
        {
        };
    }

    public override async Task<CancelUploadMultiPartResponse> CancelUploadMultiPart(
        CancelUploadMultiPartRequest request,
        ServerCallContext context)
    {
        var tasks = new List<Task>()
        {
            AbortUploadMultiPart(request.UploadId, request.ObjectKey),
            DeleteFile(request.ObjectKey)
        };

        await Task.WhenAll(tasks);

        return new CancelUploadMultiPartResponse();
    }

    private async Task AbortUploadMultiPart(string uploadId, string objectKey)
    {
        try
        {
            var abortMultipartUploadRequest = new AbortMultipartUploadRequest
            {
                BucketName = _bucketTemporary,
                Key = objectKey,
                UploadId = uploadId
            };

            await client.AbortMultipartUploadAsync(abortMultipartUploadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error aborting multipart upload");
        }
    }

    private async Task DeleteFile(string objectKey)
    {
        try
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _bucketTemporary,
                Key = objectKey,
            };
            var a = await client.DeleteObjectAsync(deleteObjectRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error  deleting file");
        }
    }

    private string GeneratePresignedUrl(
        string objectKey,
        string uploadId,
        int partNumber,
        string fileName,
        string contentType,
        int expirationHours = 6)
    {
        var request = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = _bucketTemporary,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            ContentType = contentType,
            Protocol = Protocol.HTTP,
            // Parâmetros específicos para upload multiparte
            UploadId = uploadId,
            PartNumber = partNumber,
            // Headers importantes
            Headers =
            {
                [$"x-amz-meta-{MetadataKey}"] = fileName // Metadado formatado corretamente
            }
        };

        return client.GetPreSignedURL(request);
    }


    private (long partSize, int partCount) CalculateParts(long totalSize)
    {
        const long minPartSize = 5 * 1024 * 1024; // 5MB
        const long maxPartSize = 100 * 1024 * 1024; // 100MB
        const int maxParts = 10000;

        var partSize = Math.Max(minPartSize, totalSize / maxParts);
        partSize = Math.Min(partSize, maxPartSize);
        var partCount = (int)Math.Ceiling((double)totalSize / partSize);

        return (partSize, partCount);
    }
}