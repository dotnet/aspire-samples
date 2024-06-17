using Aspire.Hosting.Lifecycle;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MetricsApp.AppHost.ContainerProxy
{
    internal sealed class ContainerProxyResourceLifecycleHook : IDistributedApplicationLifecycleHook, IAsyncDisposable
    {
        private const string NginxConfigTemplate = @"
events {}

http {
    server {
        listen 80;
        
        location / {
            proxy_pass http://{host}:{port};
        }
    }
}
";

        private readonly ILogger<ContainerProxyResourceLifecycleHook> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly string _networkNamePrefix;
        private readonly string _networkName;

        public ContainerProxyResourceLifecycleHook(
            ILogger<ContainerProxyResourceLifecycleHook> logger,
            IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;

            _networkNamePrefix = $"{hostEnvironment.ApplicationName}_Proxy_";
            _networkName = hostEnvironment.ApplicationName + Guid.NewGuid().ToString().Substring(0, 6);
        }

        public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            await CleanupNetworks(CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Creating docker network {Network}.", _networkName);
            await Cli.Wrap("docker").WithArguments($"network create {_networkName}").ExecuteAsync(cancellationToken).ConfigureAwait(false);

            foreach (var containerResource in appModel.Resources.OfType<ContainerResource>())
            {
                var proxyAnnotation = containerResource.Annotations.OfType<ContainerProxyAnnotation>().SingleOrDefault();
                if (proxyAnnotation != null)
                {
                    var confFilePath = Path.Combine(_hostEnvironment.ContentRootPath, $"{containerResource.Name}.nginx.conf");
                    proxyAnnotation.FilePath = confFilePath;
                    containerResource.Annotations.Add(new ContainerMountAnnotation(confFilePath, "/etc/nginx/nginx.conf", ContainerMountType.BindMount, true));
                }

                containerResource.Annotations.Add(new ContainerRuntimeArgsCallbackAnnotation(context =>
                {
                    context.Add("--network");
                    context.Add(_networkName);
                }));
            }
        }

        public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            foreach (var containerResource in appModel.Resources.OfType<ContainerResource>())
            {
                var proxyAnnotation = containerResource.Annotations.OfType<ContainerProxyAnnotation>().SingleOrDefault();
                if (proxyAnnotation != null && proxyAnnotation.FilePath != null)
                {
                    var endpoint = proxyAnnotation.ProxiedResource.GetEndpoint("http");
                    var configString = NginxConfigTemplate
                        .Replace("{host}", "host.docker.internal")
                        .Replace("{port}", endpoint.Port.ToString());
                    File.WriteAllText(proxyAnnotation.FilePath, configString);
                }
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await CleanupNetworks(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task CleanupNetworks(CancellationToken cancellationToken)
        {
            var oldNetworks = Cli.Wrap("docker")
                .WithArguments("network ls --format '{{.Name}}'")
                .ListenAsync(cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .OfType<StandardOutputCommandEvent>()
                .Where(e => e.Text.StartsWith(_networkNamePrefix));
            foreach (var oldNetwork in oldNetworks)
            {
                _logger.LogInformation("Deleting docker network {Network}.", oldNetwork);
                await Cli.Wrap("docker").WithArguments($"network rm {oldNetwork}").ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
