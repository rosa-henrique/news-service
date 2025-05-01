using Grpc.Health.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class ServiceReferenceExtensions
{
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services,
        string baseAddress)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        return services.AddHttpClient<TClient>(c => c.BaseAddress = new(baseAddress));
    }


    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services,
        string baseAddress, string healthRelativePath, string? healthCheckName = default,
        HealthStatus failureStatus = default)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(healthRelativePath);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        if (!Uri.IsWellFormedUriString(healthRelativePath, UriKind.Relative))
        {
            throw new ArgumentException("Health check path must be a valid relative URI.", nameof(healthRelativePath));
        }

        var uri = new Uri(baseAddress);
        var builder = services.AddHttpClient<TClient>(c => c.BaseAddress = uri);

        services.AddHealthChecks()
            .AddUrlGroup(
                new Uri(uri, healthRelativePath),
                healthCheckName ?? $"{typeof(TClient).Name}-health",
                failureStatus,
                //configureClient: (s, c) => s.GetRequiredService<IHttpClientFactory>().CreateClient
                configurePrimaryHttpMessageHandler: s =>
                    s.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler()
            );

        return builder;
    }

    public static IHttpClientBuilder AddGrpcServiceReference<TClient>(this IServiceCollection services, string address)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(address, UriKind.Absolute))
        {
            throw new ArgumentException("Address must be a valid absolute URI.", nameof(address));
        }

        var builder = services.AddGrpcClient<TClient>(o => o.Address = new(address));

        return builder;
    }

    public static IHttpClientBuilder AddGrpcServiceReference<TClient>(this IServiceCollection services, string address,
        string? healthCheckName = null, HealthStatus failureStatus = default)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(address, UriKind.Absolute))
        {
            throw new ArgumentException("Address must be a valid absolute URI.", nameof(address));
        }

        var uri = new Uri(address);
        var builder = services.AddGrpcClient<TClient>(o => o.Address = uri);

        AddGrpcHealthChecks(services, uri, healthCheckName ?? $"{typeof(TClient).Name}-health", failureStatus);

        return builder;
    }

    private static void AddGrpcHealthChecks(IServiceCollection services, Uri uri, string healthCheckName,
        HealthStatus failureStatus = default)
    {
        services.AddGrpcClient<Health.HealthClient>(o => o.Address = uri);
        services.AddHealthChecks()
            .AddCheck<GrpcServiceHealthCheck>(healthCheckName, failureStatus);
    }

    private sealed class GrpcServiceHealthCheck(Health.HealthClient healthClient) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var response = await healthClient.CheckAsync(new(), cancellationToken: cancellationToken);

            return response.Status switch
            {
                HealthCheckResponse.Types.ServingStatus.Serving => HealthCheckResult.Healthy(),
                _ => HealthCheckResult.Unhealthy()
            };
        }
    }
}