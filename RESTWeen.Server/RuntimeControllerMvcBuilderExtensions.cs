using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace RESTween.Server;

public static class RuntimeControllerMvcBuilderExtensions
{
    public static IMvcBuilder AddRuntimeControllers(this IMvcBuilder mvcBuilder)
    {
        return AddRuntimeControllers(mvcBuilder, configure: null);
    }

    public static IMvcBuilder AddRuntimeControllers(
        this IMvcBuilder mvcBuilder,
        Action<RuntimeControllerOptions>? configure)
    {
        if (mvcBuilder is null)
            throw new ArgumentNullException(nameof(mvcBuilder));

        var options = new RuntimeControllerOptions();
        foreach (var apiInterface in GetRegisteredApis(mvcBuilder.Services))
            options.AddApi(apiInterface);

        configure?.Invoke(options);

        ValidateHandlers(mvcBuilder.Services, options.ApiInterfaces);

        var assembly = RuntimeControllerAssemblyBuilder.BuildAssembly(options.ApiInterfaces);
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Add(new AssemblyPart(assembly));
        });

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
