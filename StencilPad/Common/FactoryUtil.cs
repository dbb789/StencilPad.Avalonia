using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace StencilPad.Common;

public static class FactoryUtil
{
    public static void AddFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(IServiceCollection services) where TService : class
    {
        services.AddTransient<TService>();
        services.AddSingleton<Factory<TService>>(sp => new(() => sp.GetRequiredService<TService>()));
    }
}
