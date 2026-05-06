using System;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace RESTween.Server;

public static class RestweenRuntimeControllerMvcBuilderExtensions
{
    public static IMvcBuilder AddRestweenRuntimeControllers(
        this IMvcBuilder mvcBuilder,
        Action<RestweenRuntimeControllerOptions> configure)
    {
        if (mvcBuilder is null)
            throw new ArgumentNullException(nameof(mvcBuilder));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var options = new RestweenRuntimeControllerOptions();
        configure(options);

        var assembly = RestweenRuntimeControllerAssemblyBuilder.BuildAssembly(options.ApiInterfaces);
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Add(new AssemblyPart(assembly));
        });

        return mvcBuilder;
    }
}
