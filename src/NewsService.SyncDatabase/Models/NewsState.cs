using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace NewsService.SyncDatabase.Models;

public class NewsState
{
    public string Id { get; set; } = null!;
    
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public NewsStateTypeProcess TypeProcess { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public NewsStateStatus Status { get; set; }
    public ICollection<NewsStateFile> Files { get; set; } = null!;
}

public class NewsStateFile
{
    public string FileId { get; set; } = null!;
    
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public NewsStateFileStatus Status { get; set; }
}

public enum NewsStateTypeProcess
{
    Created
}

public enum NewsStateStatus
{
    ProcessingInit,
    Success,
    Failed,
}

public enum NewsStateFileStatus
{
    Success,
    Failed,
}