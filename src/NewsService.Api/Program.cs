using System.Text.Json.Serialization;
using Amazon.S3;
using MassTransit;
using NewsService.Api.Services;
using NewsService.Contracts;
using NewsService.Postgres;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<NewsDbContext>("newsdb");

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var minioAccessKey = builder.Configuration.GetValue<string>("MINIO_ACCESS_KEY");
var minioSecretKey = builder.Configuration.GetValue<string>("MINIO_SECRET_KEY");
var minioConnectionString = builder.Configuration.GetConnectionString("minio");

builder.Services.AddScoped<IAmazonS3>(_ =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = minioConnectionString, // MinIO URL
        ForcePathStyle = true // Required for MinIO compatibility,
    };

    return new AmazonS3Client(minioAccessKey, minioSecretKey, s3Config);
});

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();
    
    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        
        configurator.Publish<ProcessNewsFiles>(x =>
        {
            x.Durable = true; // default: true
            x.ExchangeType = ExchangeType.Fanout;
        });
        
        configurator.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGrpcHealthChecksService();

app.MapGrpcService<ObjectStorageService>();
app.MapGrpcService<NewsService.Api.Services.NewsService>();

app.MapDefaultEndpoints();

app.Run();

