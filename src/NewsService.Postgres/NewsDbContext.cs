using Microsoft.EntityFrameworkCore;

namespace NewsService.Postgres;

public class NewsDbContext(DbContextOptions<NewsDbContext> options) : DbContext(options)
{
    public DbSet<Models.News> News { get; set; }
    public DbSet<Models.DocumentFile> NewsDocumentsFiles { get; set; }
    public DbSet<Models.ImageFile> NewsImagesFiles { get; set; }
    public DbSet<Models.VideoFile> NewsVideosFiles { get; set; }

}