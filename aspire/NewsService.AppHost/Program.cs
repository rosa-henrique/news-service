using NewsService.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var minioContainer = builder.AddMinioEngine("minio");

var minioAccessKey = builder.AddParameter("minioAccessKey", secret: true);
var minioSecretKey = builder.AddParameter("minioSecretKey", secret: true);
var newsApiResourceBuilder = builder.AddProject<Projects.NewsService_Api>("newsapi")
    .WithEnvironment("MINIO_ACCESS_KEY", minioAccessKey)
    .WithEnvironment("MINIO_SECRET_KEY", minioSecretKey)
    .WithReference(minioContainer)
    .WaitFor(minioContainer);
//.WithHttpsHealthCheck("/health");
//.WithHttpHealthCheck("/health");

builder.AddProject<Projects.NewsService_Web>("newsweb")
    .WithExternalHttpEndpoints()
    .WithReference(newsApiResourceBuilder)
    .WaitFor(newsApiResourceBuilder);


builder.Build().Run();