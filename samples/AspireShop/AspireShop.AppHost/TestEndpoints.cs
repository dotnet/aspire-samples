using System.Reflection;

[assembly:AssemblyMetadata("TestEndpointsTypeName", nameof(TestEndpoints))]
[assembly:AssemblyMetadata("TestEndpointsMethodName", nameof(TestEndpoints.GetTestEndpoints))]

public static class TestEndpoints
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetTestEndpoints() =>
        new Dictionary<string, IReadOnlyList<string>>
        {
            { "catalogdbmanager", ["/alive", "/health"] },
            { "catalogservice", ["/alive", "/health"] },
            // Can't send non-gRPC requests over non-TLS connection to the BasketService unless client is manually configured to use HTTP/2
            //{ "basketservice", ["/alive", "/health"] },
            { "frontend", ["/"] }
        };
}
