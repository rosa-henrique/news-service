using Amazon.S3;
using MassTransit;
using NewsService.Contracts;
using NewsService.ProcessFile.Consumers;
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
        
        configurator.Message<ProcessNewsFiles>(x => x.SetEntityName("file.processing.orchestrator"));
        
        configurator.Publish<ProcessNewsFiles>(x =>
        {
            x.Durable = true; // default: true
            //x.AutoDelete = true;
            x.ExchangeType = ExchangeType.Fanout;
        });
        
        configurator.ReceiveEndpoint("process.file", e =>
        {
            e.ConfigureConsumeTopology = false; 

            e.Bind("file.processing.orchestrator", s =>
            {
                s.ExchangeType = ExchangeType.Fanout;
            });
            
            e.Consumer<ProcessNewsFilesConsumer>(context);
        });
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();

