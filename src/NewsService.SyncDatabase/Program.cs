using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MassTransit;
using NewsService.Contracts;
using NewsService.Postgres;
using NewsService.SyncDatabase.Consumers;
using NewsService.SyncDatabase.ElasticConfig;
using NewsService.SyncDatabase.Models;
using NewsService.SyncDatabase.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<NewsDbContext>("newsdb");

builder.AddElasticsearchClient(connectionName: "newsElasticsearch", configureClientSettings: a =>
{
    a.DefaultMappingFor<NewsState>(m =>
    {
        m.IndexName("news_state")
            .IdProperty(i => i.Id);
    });

    a.DefaultMappingFor<NewsStateAudit>(m =>
    {
        m.IndexName("news_state_audit")
            .IdProperty(i => i.FileId);
    });
});

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.SetKebabCaseEndpointNameFormatter();

    busConfigurator.AddConsumer<NewsProcessingConsumer>();

    busConfigurator.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(builder.Configuration.GetConnectionString("rabbitmq"));

        configurator.ConfigureEndpoints(context);
    });
});

builder.Services.AddTransient<ISyncDatabasesService, SyncDatabasesService>();

var app = builder.Build();

var elasticClient = app.Services.GetRequiredService<ElasticsearchClient>();
await IndexManagement.CreateIndexIfNotExists(elasticClient);

app.MapGet("news",
    async (ElasticsearchClient elasticClient) =>
    {
        var response=  await elasticClient.SearchAsync<NewsState>(s => s
            .Index("news_state")
            .Query(q => q
                    .MatchAll(a => a.Boost(1.2F)) 
            )
            .Size(10000)
            .Scroll("1m"));

        return response.Documents;
    });

app.MapGet("news/audit",
    async (ElasticsearchClient elasticClient) =>
    {
        var response =  await elasticClient.SearchAsync<NewsStateAudit>(s => s
            .Index("news_state_audit")
            .Query(q => q
                .MatchAll(a => a.Boost(1.2F)) 
            )
            .Size(10000)
            .Scroll("1m"));

        return response.Documents;
    });

app.Run();