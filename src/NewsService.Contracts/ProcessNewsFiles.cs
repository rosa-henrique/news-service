namespace NewsService.Contracts;

public record ProcessNewsFiles(Guid NewsId, IReadOnlyList<ProcessFiles> Files, int CurrentFile = 0);