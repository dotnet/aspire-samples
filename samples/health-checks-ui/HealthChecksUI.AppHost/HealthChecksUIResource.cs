using System.Diagnostics;
using Aspire.Hosting.Lifecycle;

namespace HealthChecksUI;

/// <summary>
/// A container-based resource for the HealthChecksUI container.
/// See https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#HealthCheckUI
/// </summary>
/// <param name="name">The resource name.</param>
public class HealthChecksUIResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    internal int ProjectConfigIndex { get; set; }

    /// <summary>
    /// Known environment variables for the HealthChecksUI container that can be used to configure the container.
    /// Taken from https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/doc/ui-docker.md#environment-variables-table
    /// </summary>
    public static class KnownEnvVars
    {
        public const string UiPath = "ui_path";
        // These keys are taken from https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks?tab=readme-ov-file#sample-2-configuration-using-appsettingsjson
        public const string HealthChecksConfigSection = "HealthChecksUI__HealthChecks";
        public const string HealthCheckName = "Name";
        public const string HealthCheckUri = "Uri";

        internal static string GetHealthCheckNameKey(int index) => $"{HealthChecksConfigSection}__{index}__{HealthCheckName}";

        internal static string GetHealthCheckUriKey(int index) => $"{HealthChecksConfigSection}__{index}__{HealthCheckUri}";
    }
}
