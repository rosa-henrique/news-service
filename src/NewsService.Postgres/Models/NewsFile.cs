using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NewsService.Postgres.Enums;

namespace NewsService.Postgres.Models;

public class NewsFile
{
    [Key, Column("id")]
    public Guid Id { get; set; }
    
    [Column("bucket"), MaxLength(20)]
    public string Bucket { get; set; } = null!;
    
    [Column("file_path"), MaxLength(100)]
    public string FilePath { get; set; } = null!;
    
    [Column(TypeName = "varchar(24)")]
    public StatusFile Status { get; set; }  // ex: Pending, Processing, Completed, Failed
    
    [Column("error_message"), MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    [Column("create_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("news_id")]
    public Guid NewsId { get; set; }
    
    [ForeignKey(nameof(NewsId))]
    public News News { get; set; } = null!;
}

[Table("news_documents_file")]
public class DocumentFile : NewsFile
{
    [Column("files_extension"), MaxLength(10)]
    public string FileExtension { get; set; } = "pdf"; 
}

[Table("news_images_file")]
public class ImageFile : NewsFile
{
    [Column("files_extension"), MaxLength(10)]
    public string FileExtension { get; set; } = "jpg"; 
}

[Table("news_videos_file")]
public class VideoFile : NewsFile
{
    [Column("files_extension"), MaxLength(10)]
    public string FileExtension { get; set; } = "mp4";
}