using Amazon.S3;
using MassTransit;
using NewsService.Contracts;
using NewsService.Contracts.Enums;
using NewsService.ProcessFile.Consumers;
using NewsService.ProcessFile.FilesProcessors;
using NewsService.ProcessFile.Service;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
    
    busConfigurator.AddConsumer<ProcessNewsFilesConsumer>();
    
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

builder.Services.AddTransient<IFileProcessorFactory, FileProcessorFactory>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();
builder.Services.AddKeyedTransient<IFileProcessor, DocumentFileProcessor>(nameof(ProcessFilesTypes.Document));
builder.Services.AddKeyedTransient<IFileProcessor, ImageFileProcessor>(nameof(ProcessFilesTypes.Image));
builder.Services.AddKeyedTransient<IFileProcessor, VideoFileProcessor>(nameof(ProcessFilesTypes.Video));

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();

