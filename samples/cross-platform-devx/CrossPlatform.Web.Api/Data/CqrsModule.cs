using System.Reflection;
using CQRS.Mediatr.Lite;

namespace CrossPlatform.Web.Api.Data;

public static class CqrsModule
{
    public static IServiceCollection AddCqrsServices(this IServiceCollection services)
    {
        // Basic CQRS Services from underlying library
        services.AddTransient<IQueryService, QueryService>();
        services.AddTransient<IRequestHandlerResolver>(ctx => new RequestHandlerResolver(ctx.GetRequiredService));

        // Register all app specific Query Handlers using reflection
        RegisterHandlers(services);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        List<Type> handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsHandlerType(t))
            .ToList();

        foreach (Type handlerType in handlerTypes)
        {
            RegisterHandler(services, handlerType);
        }
    }

    private static bool IsHandlerType(Type type) =>
        type.BaseType is { IsGenericType: true } &&
        type.BaseType.GetGenericTypeDefinition() == typeof(QueryHandler<,>);

    private static void RegisterHandler(IServiceCollection services, Type handlerType)
    {
        Type baseType = handlerType.BaseType!;
        services.AddTransient(handlerType);
        services.AddTransient(baseType, sp => sp.GetRequiredService(handlerType));
    }
}

