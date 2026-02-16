using Microsoft.Extensions.DependencyInjection;
using RESTween.Handlers;
using System;
using System.Net.Http;

namespace RESTween
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
        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, Uri baseAddress, IRequestHandler requestHandler)
              where TInterface : class
        {

            services.AddScoped(provider =>
            {
             
                var httpClient = new HttpClient();
                httpClient.BaseAddress = baseAddress;
                return ApiClientFactory.CreateClient<TInterface>(httpClient, requestHandler); ;
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

        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, HttpClient httpClient,IRequestHandler requestHandler)
             where TInterface : class
        {

            services.AddScoped(provider =>
            {
                return ApiClientFactory.CreateClient<TInterface>(httpClient, requestHandler); ;
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TApi, THandler>(
    this IServiceCollection services,
    Uri baseAddress)
    where TApi : class
    where THandler : class, IRequestHandler
        {
            services.AddScoped<TApi>(sp =>
            {
                var handler = sp.GetRequiredService<THandler>();

                var httpClient = new HttpClient();

                httpClient.BaseAddress = baseAddress;

                return ApiClientFactory.CreateClient<TApi>(httpClient, handler);
            });

            return services;
        }


    }
}
