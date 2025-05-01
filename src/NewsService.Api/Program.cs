using NewsService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var app = builder.Build();

app.MapGrpcHealthChecksService();

app.MapGrpcService<ObjectStorageService>();

app.MapDefaultEndpoints();

app.Run();