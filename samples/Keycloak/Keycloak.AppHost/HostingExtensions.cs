namespace Aspire.Hosting;

public static class HostingExtensions
{
    /// <summary>
    /// Configures the parameter resource with a generated default value. The generated default value is stored in user secrets during local development.
    /// </summary>
    public static IResourceBuilder<ParameterResource> WithGeneratedDefault(this IResourceBuilder<ParameterResource> builder, GenerateParameterDefault generateParameterDefault)
    {
        var generatedParameter = ParameterResourceBuilderExtensions.CreateGeneratedParameter(builder.ApplicationBuilder, builder.Resource.Name, builder.Resource.Secret, generateParameterDefault);

        builder.Resource.Default = generatedParameter.Default;

        // Need to override the initial state as parameters attempt to bind from configuration first and move to an error state if it's not found.
        // This is adapted from the source of the internal static helper AddParameter(this IDistributedApplicationBuilder builder, ...) on Aspire.Hosting.ParameterResourceBuilderExtensions.
        builder.WithInitialState(new()
        {
            ResourceType = "Parameter",
            // Hide parameters by default
            IsHidden = true,
            Properties = [
                new("parameter.secret", builder.Resource.Secret.ToString()),
                new(CustomResourceKnownProperties.Source, $"Parameters:{builder.Resource.Name}")
            ]
        });

        return builder;
    }
}
