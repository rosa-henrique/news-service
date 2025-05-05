using System.ComponentModel.DataAnnotations;

namespace NewsService.Web.Models;

public class NewsFormModel
{
    [Required]
    public string Title { get; set; }
    [Required]
    public string Body { get; set; }
    [Required]
    public NewsFileFormModel Document { get; set; }
    [Required]
    public NewsFileFormModel Image { get; set; }
    [Required]
    public NewsFileFormModel Video { get; set; }
}

public class NewsFileFormModel
{
    public string FileName { get; set; }
    public string ObjectKey { get; set; }
}