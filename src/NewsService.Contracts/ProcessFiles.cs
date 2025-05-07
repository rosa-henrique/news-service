using NewsService.Contracts.Enums;

namespace NewsService.Contracts;

public record ProcessFiles(
    Guid FileId,
    ProcessFilesTypes FileType,
    string BucketTemporary,
    string TemporaryPath,
    string? PermanentBucket,
    string? PermanentPath,
    StatusProcessingFile Status,
    string? ErrorMessage);