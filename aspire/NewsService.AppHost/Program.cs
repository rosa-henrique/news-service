var builder = DistributedApplication.CreateBuilder(args);

var minioContainer = builder.AddContainer("minio", "minio/minio")
    .WithHttpEndpoint(port: 9000, name: "minio-container-port", targetPort: 9000)
    .WithHttpEndpoint(port: 9001, name: "minio-console-port", targetPort:9001)
    .WithEnvironment("MINIO_ROOT_USER", "minio")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "RunningZebraMan32332#")
    .WithEnvironment("MINIO_ADDRESS", ":9000")
    .WithEnvironment("MINIO_CONSOLE_ADDRESS", ":9001")
    .WithArgs("server", "/data");

builder.AddProject<Projects.NewsService_Api>("newsapi")
    .WaitFor(minioContainer);
    //.WithHttpsHealthCheck("/health");
    //.WithHttpHealthCheck("/health");

builder.Build().Run();