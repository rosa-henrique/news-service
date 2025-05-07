using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsService.Postgres.Models;

[Table("news")]
public class News
{
    [Key,Column("id")]
    public Guid Id { get; set; }
    
    [Column("title"), MaxLength(150)]
    public string Title { get; set; } = null!;
    
    [Column("body")]
    [DataType(DataType.Text)]
    public string Body { get; set; } = null!;

    [InverseProperty("News")]
    public DocumentFile Document { get; set; } = null!;
    
    [InverseProperty("News")]
    public ImageFile Image { get; set; } = null!;
    
    [InverseProperty("News")]
    public VideoFile Video { get; set; } = null!;
}