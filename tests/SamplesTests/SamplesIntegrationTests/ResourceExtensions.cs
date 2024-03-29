
namespace SamplesIntegrationTests;

internal static class ResourceExtensions
{
    public static string GetName(this ProjectResource project)
    {
        var metadata = project.GetProjectMetadata();
        return Path.GetFileNameWithoutExtension(metadata.ProjectPath);
    }
}
