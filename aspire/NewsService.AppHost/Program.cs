using NewsService.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var minioContainer = builder.AddMinioEngine("minio");

var minioAccessKey = builder.AddParameter("minioAccessKey", secret: true);
var minioSecretKey = builder.AddParameter("minioSecretKey", secret: true);
var bucketTemporary = builder.AddParameter("bucketTemporary");

var newsPostgresDb = builder.AddPostgres("postgres")
                .WithLifetime(ContainerLifetime.Persistent)
                .AddDatabase("newsdb");

var migrationsService = builder.AddProject<Projects.NewsService_Migrations>("migration")
    .WithReference(newsPostgresDb)
    .WaitFor(newsPostgresDb);

var newsApiResourceBuilder = builder.AddProject<Projects.NewsService_Api>("newsapi")
    .WithEnvironment("MINIO_ACCESS_KEY", minioAccessKey)
    .WithEnvironment("MINIO_SECRET_KEY", minioSecretKey)
    .WithEnvironment("BUCKET_TEMPORARY", bucketTemporary)
    .WithReference(minioContainer)
    .WithReference(newsPostgresDb)
    .WaitFor(minioContainer)
    .WaitForCompletion(migrationsService);
//.WithHttpsHealthCheck("/health");
//.WithHttpHealthCheck("/health");

builder.AddProject<Projects.NewsService_Web>("newsweb")
    .WithExternalHttpEndpoints()
    .WithReference(newsApiResourceBuilder)
    .WaitFor(newsApiResourceBuilder);


builder.Build().Run();