using InstaCrud.Handler;
using InstaCrud.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace InstaCrud.AspNetCore;

public static class InstantCrudServiceCollectionExtensions {
    public static IServiceCollection AddInstantCrud(
        this IServiceCollection services,
        Action<InstantCrudOptions> configure) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new InstantCrudOptions();
        configure(options);
        options.Validate();

        EntityRegistry registry = EntityRegistry.FromTypes(options.EntityTypes.ToArray());

        services.AddSingleton(options);
        services.AddSingleton<IEntityRegistry>(registry);
        services.AddScoped<ICrudExecutor>(serviceProvider =>
            options.ExecutorFactory!(serviceProvider, registry));
        services.AddOpenApi();

        return services;
    }
}
