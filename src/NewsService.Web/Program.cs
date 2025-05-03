using Microsoft.Extensions.Diagnostics.HealthChecks;
using NewsService.Api;
using NewsService.Web.Components;
using NewsService.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

var isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";

builder.Services.AddSingleton<ObjectStorageClient>()
    .AddGrpcServiceReference<ObjectStorage.ObjectStorageClient>($"{(isHttps ? "https" : "http")}://newsapi", failureStatus: HealthStatus.Degraded);

builder.Services.AddScoped<ObjectStorageService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();