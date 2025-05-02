using NewsService.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var minioContainer = builder.AddMinioEngine("minio");

var minioContainerEndpoint = minioContainer.GetEndpoint("minio-container-port");

var minioAccessKey = builder.AddParameter("minioAccessKey", secret: true);
var minioSecretKey = builder.AddParameter("minioSecretKey", secret: true);
builder.AddProject<Projects.NewsService_Api>("newsapi")
    .WithEnvironment("MINIO_ACCESS_KEY", minioAccessKey)
    .WithEnvironment("MINIO_SECRET_KEY", minioSecretKey)
    .WithExternalHttpEndpoints()
    .WithReference(minioContainer)
    .WaitFor(minioContainer);
//.WithHttpsHealthCheck("/health");
//.WithHttpHealthCheck("/health");

builder.Build().Run();