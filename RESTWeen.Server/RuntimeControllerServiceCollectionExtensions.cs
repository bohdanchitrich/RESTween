using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace RESTween.Server;

public static class RuntimeControllerServiceCollectionExtensions
{
    public static IMvcBuilder AddRestweenController<TApi, THandler>(
        this IServiceCollection services)
        where TApi : class
        where THandler : class, TApi
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddRuntimeController<TApi, THandler>();

        return services.AddControllers().AddRuntimeControllers();
    }

    internal static IServiceCollection AddRuntimeController<TApi, THandler>(
        this IServiceCollection services)
        where TApi : class
        where THandler : class, TApi
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var apiInterface = typeof(TApi);
        if (!apiInterface.IsInterface)
            throw new ArgumentException("Runtime controllers can only be generated for interface types.", nameof(TApi));

        services.AddScoped<TApi, THandler>();
        services.AddSingleton(new RuntimeControllerApi(apiInterface));

        return services;
    }
}
