using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aspire.Hosting.Publishing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace ApiService.Tests;

public class DistributedApplicationFactory<TEntryPoint> : IDisposable, IAsyncDisposable where TEntryPoint : class
{
    IHost? _host;

    public DistributedApplicationFactory()
    {
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        EnsureHost();
        return _host.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> created by the server associated with this <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    public virtual IServiceProvider Services
    {
        get
        {
            EnsureHost();
            return _host.Services;
        }
    }

    public DistributedApplicationModel ApplicationModel
    {
        get
        {
            EnsureHost();
            return _host.Services.GetRequiredService<DistributedApplicationModel>();
        }
    }

    public HttpClient CreateClient(string resourceName, string? endpointName = default)
    {
        EnsureHost();

        var resources = ApplicationModel.Resources;
        var resource = resources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

        if (resource is null)
        {
            throw new ArgumentException($"Resource {resourceName} not found", nameof(resourceName));
        }

        if (!resource.TryGetAllocatedEndPoints(out var endpoints))
        {
            throw new InvalidOperationException($"Cannot create a client for resource {resourceName} because it has no allocated endpoints.");
        }

        AllocatedEndpointAnnotation? endpoint = null;

        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = endpoints.FirstOrDefault(e => string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                throw new ArgumentException($"Endpoint '{endpointName}' for resource '{resourceName}' not found", nameof(endpointName));
            }
        }
        else
        {
            endpoint = endpoints.FirstOrDefault(e =>
                string.Equals(e.UriScheme, "http", StringComparison.OrdinalIgnoreCase) || string.Equals(e.UriScheme, "https", StringComparison.OrdinalIgnoreCase));

            if (endpoint is null)
            {
                throw new InvalidOperationException($"Cannot create a client for resource {resourceName} because it has no allocated HTTP endpoints.");
            }
        }

        var clientFactory = _host.Services.GetRequiredService<IHttpClientFactory>();

        var client = clientFactory.CreateClient();
        client.BaseAddress = new(endpoint.UriString);

        return client;
    }

    protected virtual IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Patch DcpOptions configuration
            var dcpOptionsType = typeof(DistributedApplication).Assembly.GetType("Aspire.Hosting.Dcp.DcpOptions")!;
            var configureDcpOptionsType = typeof(IConfigureOptions<>).MakeGenericType(dcpOptionsType);
            services.RemoveAll(configureDcpOptionsType);

            var patchedDcpOptionsOpenType = typeof(PatchedDcpOptions<>);
            var patchedDcpOptionsType = patchedDcpOptionsOpenType.MakeGenericType(typeof(TEntryPoint), dcpOptionsType);
            services.AddSingleton(configureDcpOptionsType, patchedDcpOptionsType);

            services.AddHttpClient();
            services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
        });

        var host = builder.Build();

        return host;
    }

    [MemberNotNull(nameof(_host))]
    private void EnsureHost()
    {
        if (_host is not null)
        {
            return;
        }

        EnsureDepsFile();

        var deferredHostBuilder = new DeferredHostBuilder();
        deferredHostBuilder.UseEnvironment(Environments.Development);
        // There's no helper for UseApplicationName, but we need to 
        // set the application name to the target entry point 
        // assembly name.
        deferredHostBuilder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { HostDefaults.ApplicationKey, typeof(TEntryPoint).Assembly.GetName()?.Name ?? string.Empty }
                });
        });
        // This helper call does the hard work to determine if we can fallback to diagnostic source events to get the host instance
        var factory = HostFactoryResolver.ResolveHostFactory(
            typeof(TEntryPoint).Assembly,
            stopApplication: false,
            configureHostBuilder: deferredHostBuilder.ConfigureHostBuilder,
            entrypointCompleted: deferredHostBuilder.EntryPointCompleted);

        if (factory is not null)
        {
            // If we have a valid factory it means the specified entry point's assembly can potentially resolve the IHost
            // so we set the factory on the DeferredHostBuilder so we can invoke it on the call to IHostBuilder.Build.
            deferredHostBuilder.SetHostFactory(factory);

            _host = CreateHost(deferredHostBuilder);
            return;
        }

        throw new InvalidOperationException("Could not intercept host building");
    }

    private static void EnsureDepsFile()
    {
        if (typeof(TEntryPoint).Assembly.EntryPoint == null)
        {
            throw new InvalidOperationException($"Assembly of specified type {typeof(TEntryPoint).Name} does not have an entry point.");
        }

        var depsFileName = $"{typeof(TEntryPoint).Assembly.GetName().Name}.deps.json";
        var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
        if (!depsFile.Exists)
        {
            throw new InvalidOperationException($"Missing deps file '{Path.GetFileName(depsFile.FullName)}'. Make sure the project has been built.");
        }
    }

    public void Dispose()
    {
        _host?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await _host.StopAsync();
            await asyncDisposable.DisposeAsync();
        }
    }

    class PatchedDcpOptions<TOptions>(IConfiguration configuration) : IConfigureOptions<TOptions> where TOptions : class
    {
        private const string DcpCliPathMetadataKey = "DcpCliPath";
        private const string DcpExtensionsPathMetadataKey = "DcpExtensionsPath";
        private const string DcpBinPathMetadataKey = "DcpBinPath";

        //public void Configure(DcpOptions options)
        //{
        //    options.CliPath = null;
        //    options.ExtensionsPath = null;
        //    options.BinPath = null;
        //}

        public void Configure(TOptions options)
        {
            ApplyApplicationConfiguration(options);
        }

        private void ApplyApplicationConfiguration(TOptions options)
        {
            var dcpPublisherConfiguration = configuration.GetSection("DcpPublisher");
            var publishingConfiguration = configuration.GetSection("Publishing");

            string? publisher = publishingConfiguration[nameof(PublishingOptions.Publisher)];
            string? cliPath;

            if (publisher is not null && publisher != "dcp")
            {
                // If DCP is not set as the publisher, don't calculate the DCP config
                return;
            }

            if (!string.IsNullOrEmpty(dcpPublisherConfiguration["CliPath"]))
            {
                // If an explicit path to DCP was provided from configuration, don't try to resolve via assembly attributes
                cliPath = dcpPublisherConfiguration["CliPath"];
                typeof(TOptions).GetProperty("CliPath")!.SetValue(options, cliPath);
            }
            else
            {
                var entryPointAssembly = typeof(TEntryPoint).Assembly;
                var assemblyMetadata = entryPointAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
                cliPath = GetMetadataValue(assemblyMetadata, DcpCliPathMetadataKey);
                typeof(TOptions).GetProperty("CliPath")!.SetValue(options, cliPath);
                typeof(TOptions).GetProperty("ExtensionsPath")!.SetValue(options, GetMetadataValue(assemblyMetadata, DcpExtensionsPathMetadataKey));
                typeof(TOptions).GetProperty("BinPath")!.SetValue(options, GetMetadataValue(assemblyMetadata, DcpBinPathMetadataKey));
            }

            if (string.IsNullOrEmpty(cliPath))
            {
                throw new InvalidOperationException($"Could not resolve the path to the Aspire application host. The application cannot be run without it.");
            }
        }

        private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
        {
            return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
