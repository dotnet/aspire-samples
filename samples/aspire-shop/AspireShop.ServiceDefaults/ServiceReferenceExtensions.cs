using Grpc.Health.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace System.Net.Http;

public static class ServiceReferenceExtensions
{
    /// <summary>
    /// Adds an HTTP service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with a base address. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the typed client. The type specified will be registered in the service collection as a transient service. See
    /// <see cref="Microsoft.Extensions.Http.ITypedHttpClientFactory{TClient}"/> for more details about authoring typed clients.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="baseAddress">The value to assign to the <see cref="HttpClient.BaseAddress"/> property of the typed client's injected <see cref="HttpClient"/> instance.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> is not a valid URI value.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services, string baseAddress)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        return services.AddHttpClient<TClient>(c => c.BaseAddress = new(baseAddress));
    }


    /// <summary>
    /// Adds an HTTP service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with a base address and health check. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the typed client. The type specified will be registered in the service collection as a transient service. See
    /// <see cref="Microsoft.Extensions.Http.ITypedHttpClientFactory{TClient}"/> for more details about authoring typed clients.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="baseAddress">The value to assign to the <see cref="HttpClient.BaseAddress"/> property of the typed client's injected <see cref="HttpClient"/> instance.</param>
    /// <param name="healthRelativePath">The relative path of the health check endpoint for this HTTP service.</param>
    /// <param name="healthCheckName">The name for the health check. Defaults to <c>"{typeof(TClient).Name}-health"</c> if not provided.</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> that should be reported if the health check fails. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> or <paramref name="healthRelativePath"/> are not valid URI values.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services, string baseAddress, string healthRelativePath, string? healthCheckName = default, HealthStatus failureStatus = default)
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
                configurePrimaryHttpMessageHandler: s => s.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler()
                );

        return builder;
    }

    /// <summary>
    /// Adds a gRPC service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with an address. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="address">
    /// The value to assign to the <see cref="Grpc.Net.ClientFactory.GrpcClientFactoryOptions.Address"/> property of the typed gRPC client's injected
    /// <see cref="Grpc.Net.ClientFactory.GrpcClientFactoryOptions"/> instance.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address"/> is not a valid URI value.</exception>
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

    /// <summary>
    /// Adds a gRPC service reference. Configures a binding between the <typeparamref name="TClient"/> type and a named <see cref="HttpClient"/>
    /// with an address and gRPC-based health check. The client name will be set to the type name of <typeparamref name="TClient"/>.
    /// </summary>
    /// <remarks>
    /// Note that the gRPC service must be configured to use gRPC health checks. See https://learn.microsoft.com/aspnet/core/grpc/health-checks for more details.
    /// </remarks>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="address">
    /// The value to assign to the <see cref="Grpc.Net.ClientFactory.GrpcClientFactoryOptions.Address"/> property of the typed gRPC client's injected
    /// <see cref="Grpc.Net.ClientFactory.GrpcClientFactoryOptions"/> instance.
    /// </param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> that should be reported if the health check fails. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="address"/> is not a valid URI value.</exception>
    public static IHttpClientBuilder AddGrpcServiceReference<TClient>(this IServiceCollection services, string address, string? healthCheckName = null, HealthStatus failureStatus = default)
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

    private static void AddGrpcHealthChecks(IServiceCollection services, Uri uri, string healthCheckName, HealthStatus failureStatus = default)
    {
        services.AddGrpcClient<Health.HealthClient>(o => o.Address = uri);
        services.AddHealthChecks()
            .AddCheck<GrpcServiceHealthCheck>(healthCheckName, failureStatus);
    }

    private sealed class GrpcServiceHealthCheck(Health.HealthClient healthClient) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
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
