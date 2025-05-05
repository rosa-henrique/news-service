using System.Buffers;
using System.Net.Http.Headers;
using System.Web;

namespace NewsService.Web.Services;

public class ObjectStorageService(IHttpClientFactory clientFactor, ILogger<ObjectStorageService> logger)
{
    private readonly HttpClient _httpClient = clientFactor.CreateClient();

    public async Task<bool> UploadFile(Stream fileStream, string contentType, string fileName, string preSignedUrl,
        IProgress<int> progress = null, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent((int)fileStream.Length);
        var success = false;

        try
        {
            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            var readOnlyMemory = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);

            using var content = new StreamContent(new MemoryStream(buffer, 0, bytesRead));
            // Configurar headers para upload
            content.Headers.Add("x-amz-meta-original-filename", fileName);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _httpClient.PutAsync(preSignedUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            progress?.Report(100);

            success = true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return success;
    }

    public async Task<(bool, IDictionary<int, string>)> UploadFileMultiPart(
        Stream fileStream,
        string contentType,
        string fileName,
        string[] preSignedUrls,
        IProgress<int> progress = null,
        CancellationToken cancellationToken = default,
        int chunkSize = 10 * 1024 * 1024, // 10MB padrão (mínimo 5MB)
        int? totalChunks = null,
        int maxRetries = 3)
    {
        var indexETag = new Dictionary<int, string>();

        // Validação do chunkSize
        if (chunkSize < 5 * 1024 * 1024)
            throw new ArgumentException("ChunkSize deve ser pelo menos 5MB");

        try
        {
            var fileSize = fileStream.Length;
            totalChunks ??= (int)Math.Ceiling((double)fileSize / chunkSize);
            var uploadedBytes = 0L;

            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);

            try
            {
                for (var chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Ler EXATAMENTE chunkSize bytes (exceto última parte)
                    var bytesRead = 0;
                    var totalRead = 0;
                    while (totalRead < chunkSize &&
                           (bytesRead = await fileStream.ReadAsync(
                               buffer,
                               totalRead,
                               chunkSize - totalRead,
                               cancellationToken)) > 0)
                    {
                        totalRead += bytesRead;
                    }

                    var isLastChunk = chunkNumber == totalChunks - 1;
                    if (!isLastChunk && totalRead < chunkSize)
                        throw new InvalidOperationException("Não foi possível ler chunk completo");

                    var chunkData = new ArraySegment<byte>(buffer, 0, totalRead);

                    var (success, etag) = await UploadChunkWithRetryAsync(
                        chunkData,
                        preSignedUrls[chunkNumber],
                        contentType,
                        maxRetries,
                        fileName,
                        cancellationToken);

                    if (!success) return (false, indexETag);

                    uploadedBytes += totalRead;
                    progress?.Report((int)((double)uploadedBytes / fileSize * 100));

                    indexETag.Add(chunkNumber + 1, etag); // Part numbers começam em 1
                }

                return (true, indexETag);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Upload cancelado");
            return (false, indexETag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no upload");
            return (false, indexETag);
        }
    }

    private async Task<(bool, string)> UploadChunkWithRetryAsync(
        ArraySegment<byte> chunkData,
        string preSignedUrl,
        string contentType,
        int maxRetries,
        string fileName,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var success = false;
        var eTag = string.Empty;

        while (!success && retryCount <= maxRetries)
        {
            try
            {
                using var content = new ByteArrayContent(chunkData.Array, chunkData.Offset, chunkData.Count);
                // Configurar headers para upload parcial
                content.Headers.Add("x-amz-meta-original-filename", fileName);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                var response = await _httpClient.PutAsync(preSignedUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                success = true;
                var etag = response.Headers.GetValues("ETag");
                eTag = etag.FirstOrDefault();
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                logger.LogWarning(ex, "Chunk upload failed, retrying...");
                await Task.Delay(CalculateRetryDelay(retryCount), cancellationToken);
                retryCount++;
            }
        }

        return (success, eTag);
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        return TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
    }
}