using NewsService.Contracts;

namespace NewsService.ProcessFile.FilesProcessors;

public interface IFileProcessor
{
    Task<ProcessFiles> ProcessFile(ProcessFiles processFiles, string folder);
}