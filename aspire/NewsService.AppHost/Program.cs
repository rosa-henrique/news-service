using NewsService.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var minioContainer = builder.AddMinioEngine("minio");

var minioAccessKey = builder.AddParameter("minioAccessKey", secret: true);
var minioSecretKey = builder.AddParameter("minioSecretKey", secret: true);
var bucketTemporary = builder.AddParameter("bucketTemporary");
var bucketPermanent = builder.AddParameter("bucketPermanent");

var newsPostgresDb = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050))
    .AddDatabase("newsdb");

var elasticsearch = builder.AddElasticsearch("newsElasticsearch")
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin(port: 8002)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);

var migrationsService = builder.AddProject<Projects.NewsService_Migrations>("migration")
    .WithReference(newsPostgresDb)
    .WaitFor(newsPostgresDb);


var newsApiResourceBuilder = builder.AddProject<Projects.NewsService_Api>("newsapi")
    .WithEnvironment("MINIO_ACCESS_KEY", minioAccessKey)
    .WithEnvironment("MINIO_SECRET_KEY", minioSecretKey)
    .WithEnvironment("BUCKET_TEMPORARY", bucketTemporary)
    .WithReference(minioContainer)
    .WithReference(newsPostgresDb)
    .WithReference(rabbitmq)
    .WaitFor(minioContainer)
    .WaitFor(rabbitmq)
    .WaitForCompletion(migrationsService);
//.WithHttpsHealthCheck("/health");
//.WithHttpHealthCheck("/health");

builder.AddProject<Projects.NewsService_ProcessFile>("newsprocessfile")
    .WithEnvironment("MINIO_ACCESS_KEY", minioAccessKey)
    .WithEnvironment("MINIO_SECRET_KEY", minioSecretKey)
    .WithEnvironment("BUCKET_TEMPORARY", bucketTemporary)
    .WithEnvironment("BUCKET_PERMANENT", bucketPermanent)
    .WithReference(minioContainer)
    .WithReference(rabbitmq)
    .WaitFor(minioContainer)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.NewsService_Web>("newsweb")
    .WithExternalHttpEndpoints()
    .WithReference(newsApiResourceBuilder)
    .WaitFor(newsApiResourceBuilder);

builder.AddProject<Projects.NewsService_SyncDatabase>("newsSyncDatabase")
    .WithReference(newsPostgresDb)
    .WithReference(elasticsearch)
    .WithReference(rabbitmq)
    .WaitFor(elasticsearch)
    .WaitFor(rabbitmq)
    .WaitForCompletion(migrationsService);

builder.Build().Run();