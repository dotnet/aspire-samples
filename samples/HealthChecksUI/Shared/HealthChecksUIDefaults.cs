namespace HealthChecksUI;

public static class HealthChecksUIDefaults
{
    public const string ContainerImageName = "xabarilcoding/healthchecksui";
    public const int ContainerPort = 80;
    public const string ProbePath = "/healthz";
    public const string InternalEndpointName = "internalhealthchecks";
}