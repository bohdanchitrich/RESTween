using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace RESTween.Server;

public static class RuntimeControllerMvcBuilderExtensions
{
    internal static IMvcBuilder AddRuntimeControllers(this IMvcBuilder mvcBuilder)
    {
        return AddRuntimeControllers(mvcBuilder, configure: null);
    }

    internal static IMvcBuilder AddRuntimeControllers(
        this IMvcBuilder mvcBuilder,
        Action<RuntimeControllerOptions>? configure)
    {
        if (mvcBuilder is null)
            throw new ArgumentNullException(nameof(mvcBuilder));

        var options = new RuntimeControllerOptions();
        foreach (var apiInterface in GetRegisteredApis(mvcBuilder.Services))
            options.AddApi(apiInterface);

        configure?.Invoke(options);

        var apiInterfaces = GetNewApis(mvcBuilder.Services, options.ApiInterfaces).ToArray();
        if (apiInterfaces.Length == 0)
            return mvcBuilder;

        ValidateHandlers(mvcBuilder.Services, apiInterfaces);

        var assembly = RuntimeControllerAssemblyBuilder.BuildAssembly(apiInterfaces);
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Add(new AssemblyPart(assembly));
        });

        foreach (var apiInterface in apiInterfaces)
            mvcBuilder.Services.AddSingleton(new RuntimeControllerGeneratedApi(apiInterface));

        return mvcBuilder;
    }

    private static IEnumerable<Type> GetRegisteredApis(IServiceCollection services)
    {
        return services
            .Where(service => service.ServiceType == typeof(RuntimeControllerApi))
            .Select(service => service.ImplementationInstance)
            .OfType<RuntimeControllerApi>()
            .Select(registration => registration.ApiInterface);
    }

    private static IEnumerable<Type> GetNewApis(IServiceCollection services, IReadOnlyList<Type> apiInterfaces)
    {
        var generatedApis = services
            .Where(service => service.ServiceType == typeof(RuntimeControllerGeneratedApi))
            .Select(service => service.ImplementationInstance)
            .OfType<RuntimeControllerGeneratedApi>()
            .Select(registration => registration.ApiInterface)
            .ToHashSet();

        return apiInterfaces.Where(apiInterface => !generatedApis.Contains(apiInterface));
    }

    private static void ValidateHandlers(IServiceCollection services, IReadOnlyList<Type> apiInterfaces)
    {
        foreach (var apiInterface in apiInterfaces)
        {
            var hasHandler = services.Any(service => service.ServiceType == apiInterface);
            if (!hasHandler)
            {
                throw new InvalidOperationException(
                    $"No service is registered for '{apiInterface.FullName}'. Register a handler with AddRuntimeController<TApi, THandler>() or AddScoped<TApi, THandler>().");
            }
        }
    }
}
