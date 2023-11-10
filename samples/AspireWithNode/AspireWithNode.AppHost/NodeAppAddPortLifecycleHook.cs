using Aspire.Hosting.Lifecycle;

internal class NodeAppAddPortLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var nodeApps = appModel.Resources.OfType<NodeAppResource>();
        foreach (var app in nodeApps)
        {
            if (app.TryGetServiceBindings(out var bindings))
            {
                var envAnnoation = new EnvironmentCallbackAnnotation(env =>
                {
                    var multiBindings = bindings.Count() > 1;

                    if (multiBindings)
                    {
                        foreach (var binding in bindings)
                        {
                            var serviceName = multiBindings ? $"{app.Name}_{binding.Name}" : app.Name;
                            env[$"PORT_{binding.Name.ToUpperInvariant()}"] = $"{{{{- portForServing \"{serviceName}\" -}}}}";
                        }
                        
                    }
                    else
                    {
                        env["PORT"] = $"{{{{- portForServing \"{app.Name}\" -}}}}";
                    }
                });

                app.Annotations.Add(envAnnoation);
            }
        }

        return Task.CompletedTask;
    }
}
