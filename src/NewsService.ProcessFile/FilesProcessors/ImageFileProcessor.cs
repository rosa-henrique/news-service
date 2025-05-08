using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using NewsService.Contracts;
using NewsService.Contracts.Enums;

namespace NewsService.ProcessFile.FilesProcessors;

public class ImageFileProcessor(IAmazonS3 amazonClient, IConfiguration configuration) : IFileProcessor
{
    private readonly string _bucketPermanent = configuration.GetValue<string>("BUCKET_PERMANENT")!;

    public async Task<ProcessFiles> ProcessFile(ProcessFiles processFiles, string folder)
    {
        try
        {
            var copyObjectRequest = new CopyObjectRequest()
            {
                SourceBucket = processFiles.BucketTemporary,
                SourceKey = processFiles.TemporaryPath,
                DestinationBucket = _bucketPermanent,
                DestinationKey = $"{folder}/{processFiles.TemporaryPath}"
            };

            var response = await amazonClient.CopyObjectAsync(copyObjectRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
                return processFiles with
                {
                    PermanentBucket = _bucketPermanent,
                    PermanentPath = copyObjectRequest.DestinationKey,
                    Status = StatusProcessingFile.Completed,
                };

            throw new Exception($"Couldn't copy object: {response.HttpStatusCode}");
            
        }
        catch (Exception ex)
        {
            return processFiles with
            {
                ErrorMessage = ex.Message,
                Status = StatusProcessingFile.Failed,
            };
        }
    }
}