using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class MailDevResourceBuilderExtensions
{
    public static IResourceBuilder<MailDevResource> AddMailDev(this IDistributedApplicationBuilder builder, string name, int? httpPort = null, int? smtpPort = null)
    {
        var resource = new MailDevResource(name);
        return builder.AddResource(resource)
                      .WithImage(MailDevContainerImageTags.Image)
                      .WithImageRegistry(MailDevContainerImageTags.Registry)
                      .WithImageTag(MailDevContainerImageTags.Tag)
                      .WithHttpEndpoint(targetPort: 1080, port: httpPort, name: MailDevResource.HttpEndpointName)
                      .WithEndpoint(targetPort: 1025, port: smtpPort, name: MailDevResource.SmtpEndpointName);
    }
}
