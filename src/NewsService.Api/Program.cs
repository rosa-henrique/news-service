using Minio;
using NewsService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var minioAccessKey = builder.Configuration.GetValue<string>("MINIO_ACCESS_KEY");
var minioSecretKey = builder.Configuration.GetValue<string>("MINIO_SECRET_KEY");
var minioConnectionString = builder.Configuration.GetConnectionString("minio");

builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(new Uri(minioConnectionString))
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(false)
    .Build());

var app = builder.Build();

app.MapGrpcHealthChecksService();

app.MapGrpcService<ObjectStorageService>();

app.MapDefaultEndpoints();

app.Run();