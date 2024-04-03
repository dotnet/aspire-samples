using System.Reflection;

[assembly:AssemblyMetadata("TestEndpointsTypeName", nameof(TestEndpoints))]
[assembly:AssemblyMetadata("TestEndpointsMethodName", nameof(TestEndpoints.GetTestEndpoints))]

public static class TestEndpoints
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetTestEndpoints() =>
        new Dictionary<string, IReadOnlyList<string>>
        {
            { "weatherapi", ["/alive", "/health"] },
            { "frontend", ["/"] }
        };
}
