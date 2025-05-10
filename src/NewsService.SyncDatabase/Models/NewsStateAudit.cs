using System.Text.Json.Serialization;

namespace NewsService.SyncDatabase.Models;

public class NewsStateAudit
{
    public string Id { get; set; }
    public string NewsId { get; set; }
    public string FileId { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public NewsStateStatus Status { get; set; }
}