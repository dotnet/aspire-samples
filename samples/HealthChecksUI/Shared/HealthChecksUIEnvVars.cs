namespace HealthChecksUI;

public static class HealthChecksUIEnvVars
{
    public const string InternalUrl = "INTERNAL_HEALTHCHECKS_URL";
    public const string UiPath = "ui_path";
    public const string RootConfigurationKeyPrefix = "HealthChecksUI__";
    public const string HealthChecksConfigurationKeyPrefix = RootConfigurationKeyPrefix + "HealthChecks__";
    public const string HealthCheckConfigurationName = "Name";
    public const string HealthCheckConfigurationUri = "Uri";
}
