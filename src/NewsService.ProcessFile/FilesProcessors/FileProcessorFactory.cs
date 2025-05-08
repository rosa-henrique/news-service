using NewsService.Contracts.Enums;

namespace NewsService.ProcessFile.FilesProcessors;

public interface IFileProcessorFactory
{
    IFileProcessor GetProcessor(ProcessFilesTypes fileType);
}

public class FileProcessorFactory(IServiceProvider serviceProvider) : IFileProcessorFactory
{
    public IFileProcessor GetProcessor(ProcessFilesTypes fileType)
    {
        return serviceProvider.GetRequiredKeyedService<IFileProcessor>(fileType.ToString());
    }
}