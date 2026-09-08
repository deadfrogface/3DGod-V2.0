using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGod.Infrastructure;

public static class ThreeDGodComposition
{
    /// <summary>
    /// Registers only services that have a real implementation.
    /// Unimplemented capability interfaces are intentionally omitted.
    /// </summary>
    public static IServiceCollection AddThreeDGodCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ConfigService>();
        services.AddSingleton<PresetService>();
        services.AddSingleton<IBlenderOperations, LegacyBlenderBackend>();
        services.AddSingleton<IFeatureAvailabilityService, FeatureAvailabilityService>();
        services.AddSingleton<CharacterSystem>();
        services.AddSingleton<ICharacterModelService, CharacterModelServiceAdapter>();
        return services;
    }
}
