using System.Buffers;
using System.Net.Http.Headers;
using System.Web;

namespace NewsService.Web.Services;

public class ObjectStorageService(IHttpClientFactory clientFactor, ILogger<ObjectStorageService> logger)
{
    private readonly HttpClient _httpClient = clientFactor.CreateClient();

    public async Task<bool> UploadFile(
        Stream fileStream,
        string contentType,
        string preSignedUrl,
        IProgress<int> progress = null,
        CancellationToken cancellationToken = default,
        int chunkSize = 10 * 1024 * 1024,
        int maxRetries = 3)
    {
        try
        {
            var fileSize = fileStream.Length;
            var totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);
            var uploadedBytes = 0L;

            // Buffer pool para otimizar memória
            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
            
            try
            {
                for (var chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, cancellationToken);
                    var chunkData = new ArraySegment<byte>(buffer, 0, bytesRead);

                    var success = await UploadChunkWithRetryAsync(
                        chunkData,
                        preSignedUrl,
                        contentType,
                        uploadedBytes,
                        fileSize,
                        maxRetries,
                        cancellationToken);

                    if (!success) return false;

                    uploadedBytes += bytesRead;
                    progress?.Report((int)((double)uploadedBytes / fileSize * 100));
                }

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Upload canceled");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload error");
            return false;
        }
    }
    
    private async Task<bool> UploadChunkWithRetryAsync(
        ArraySegment<byte> chunkData,
        string preSignedUrl,
        string contentType,
        long offset,
        long totalSize,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var success = false;

        while (!success && retryCount <= maxRetries)
        {
            try
            {
                using (var content = new ByteArrayContent(chunkData.Array, chunkData.Offset, chunkData.Count))
                {
                    // Configurar headers para upload parcial
                    content.Headers.Add("Content-Range", $"bytes {offset}-{offset + chunkData.Count - 1}/{totalSize}");
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    var response = await _httpClient.PutAsync(preSignedUrl, content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    success = true;
                }
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                logger.LogWarning(ex, "Chunk upload failed, retrying...");
                await Task.Delay(CalculateRetryDelay(retryCount), cancellationToken);
                retryCount++;
            }
        }

        return success;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        return TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
    }
}