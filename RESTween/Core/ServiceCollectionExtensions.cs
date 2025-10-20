using Microsoft.Extensions.DependencyInjection;
using RESTween.Client;
using RESTween.Client.Handlers;
using RESTween.Server;
using System;
using System.Net.Http;

namespace RESTween.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, Uri baseAddress)
                     where TInterface : class
        {

            services.AddScoped(provider =>
            {
                var handler = provider.GetService<IRequestHandler>();
                if (handler == null)
                {
                    handler = new DefaultRequestHandler();
                }
                var httpClient = new HttpClient();
                httpClient.BaseAddress = baseAddress;
                return ApiClientFactory.CreateClient<TInterface>(httpClient, handler); ;
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, HttpClient httpClient)
               where TInterface : class
        {
            services.AddScoped(provider =>
            {
                var handler = provider.GetService<IRequestHandler>();
                if (handler == null)
                {
                    handler = new DefaultRequestHandler();
                }
                return ApiClientFactory.CreateClient<TInterface>(httpClient, handler); ;
            });

            return services;
        }


        public static IServiceCollection AddApiDispatcher<TInterface, TImplementation>(this IServiceCollection services) 
            where TImplementation : class, TInterface where TInterface : class
        {
            services.AddScoped<TInterface, TImplementation>();

            services.AddScoped(provider =>
            {
                var implementation = provider.GetRequiredService<TInterface>();
                return ApiDispatcherFactory.CreateApiDispatcher(implementation); ;
            });
            return services;
        }



    }
}
