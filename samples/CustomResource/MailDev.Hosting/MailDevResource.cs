namespace Aspire.Hosting.ApplicationModel;

public class MailDevResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string SmtpEndpointName = "smtp";
    internal const string HttpEndpointName = "http";

    private EndpointReference? _smtpReference;

    public EndpointReference SmtpEndpoint => _smtpReference ??= new(this, SmtpEndpointName);

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create(
        $"smtp://{SmtpEndpoint.Property(EndpointProperty.Host)}:{SmtpEndpoint.Property(EndpointProperty.Port)}"
        );
}
