using MassTransit;
using NewsService.Contracts;

namespace NewsService.ProcessFile.Consumers;

public class ProcessNewsFilesConsumer : IConsumer<ProcessNewsFiles>
{
    public async Task Consume(ConsumeContext<ProcessNewsFiles> context)
    {
        var news = context.Message;
        
        if(news.CurrentFile < 0)
            throw new ArgumentOutOfRangeException("news.CurrentFile");

        if (news.CurrentFile >= news.Files.Count)
            return;

        var newMessage = news with
        {
            CurrentFile = news.CurrentFile + 1
        };
        
        await context.Publish(newMessage);
    }
}