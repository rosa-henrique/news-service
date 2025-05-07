using Microsoft.EntityFrameworkCore;
using NewsService.Migrations;
using NewsService.Postgres;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<ApiDbInitializer>();

builder.AddServiceDefaults();

builder.Services.AddDbContextPool<NewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("newsdb"), sqlOptions =>
        sqlOptions.MigrationsAssembly("NewsService.Migrations")
    ));

builder.EnrichNpgsqlDbContext<NewsDbContext>();

var app = builder.Build();


app.Run();

