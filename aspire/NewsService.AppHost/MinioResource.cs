namespace NewsService.AppHost;

public class MinioEngineResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string ConsoleEndpoint = "minio-console-port";
    internal const string ContainerEndpoint = "minio-container-port";
    private EndpointReference? _primaryEndpoint;

    internal bool IsSslEnabled { get; set; } = false;

    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new EndpointReference(this, ConsoleEndpoint);
    
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{(IsSslEnabled ? "https" : "http")}://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}"
        );
}

public static class MinioEngineResourceExtensions
{
    public static IResourceBuilder<MinioEngineResource> AddMinioEngine(
        this IDistributedApplicationBuilder builder,
        string name,
        int? portConsole = null,
        int? portContainer = null)
    {
        var resource = new MinioEngineResource(name);

        resource.IsSslEnabled = builder.ExecutionContext.IsPublishMode;

        return builder.AddResource(resource)
            .WithImage("minio/minio")
            .WithImageTag(builder.Configuration["Minio:ImageTag"] ?? "latest")
            .WithHttpEndpoint(port: portConsole ?? 9000, name: MinioEngineResource.ConsoleEndpoint, targetPort: 9000)
            .WithHttpEndpoint(port: portContainer ?? 9001, name: MinioEngineResource.ContainerEndpoint, targetPort: 9001)
            .WithEnvironment("MINIO_ROOT_USER", builder.Configuration["Minio:User"])
            .WithEnvironment("MINIO_ROOT_PASSWORD", builder.Configuration["Minio:Password"])
            .WithEnvironment("MINIO_ADDRESS", builder.Configuration["Minio:Port"])
            .WithEnvironment("MINIO_CONSOLE_ADDRESS", builder.Configuration["Minio:ConsolePort"])
            //.WithVolume("/minio/data")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithArgs("server", "/data");
    }
}