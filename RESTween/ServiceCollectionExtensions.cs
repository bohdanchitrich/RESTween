using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTween.Building;
using RESTween.Handlers;
using System;
using System.Net.Http;

namespace RESTween
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRestweenRequestBuilding(this IServiceCollection services)
        {
            services.TryAddSingleton<HttpMethodMetadataReader>();
            services.TryAddSingleton<IRestweenValueFormatter, DefaultRestweenValueFormatter>();
            services.TryAddSingleton<IRestweenContentSerializer, SystemTextJsonRestweenContentSerializer>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRestweenParameterBinder, MultipartParameterBinder>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRestweenParameterBinder, HeaderParameterBinder>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRestweenParameterBinder, RouteParameterBinder>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRestweenParameterBinder, QueryParameterBinder>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRestweenParameterBinder, BodyParameterBinder>());

            services.TryAddSingleton<IRestweenRequestBuilder, DefaultRestweenRequestBuilder>();
            return services;
        }

        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, Uri baseAddress)
            where TInterface : class
        {
            services.AddRestweenRequestBuilding();

            services.AddScoped(provider =>
            {
                var handler = provider.GetService<IRequestHandler>() ?? new DefaultRequestHandler();
                var requestBuilder = provider.GetRequiredService<IRestweenRequestBuilder>();
                var httpClient = new HttpClient { BaseAddress = baseAddress };

                return ApiClientFactory.CreateClient<TInterface>(httpClient, handler, requestBuilder);
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TInterface>(
            this IServiceCollection services,
            Uri baseAddress,
            IRequestHandler requestHandler)
            where TInterface : class
        {
            services.AddRestweenRequestBuilding();

            services.AddScoped(provider =>
            {
                var requestBuilder = provider.GetRequiredService<IRestweenRequestBuilder>();
                var httpClient = new HttpClient { BaseAddress = baseAddress };

                return ApiClientFactory.CreateClient<TInterface>(httpClient, requestHandler, requestBuilder);
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TInterface>(this IServiceCollection services, HttpClient httpClient)
            where TInterface : class
        {
            services.AddRestweenRequestBuilding();

            services.AddScoped(provider =>
            {
                var handler = provider.GetService<IRequestHandler>() ?? new DefaultRequestHandler();
                var requestBuilder = provider.GetRequiredService<IRestweenRequestBuilder>();

                return ApiClientFactory.CreateClient<TInterface>(httpClient, handler, requestBuilder);
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TInterface>(
            this IServiceCollection services,
            HttpClient httpClient,
            IRequestHandler requestHandler)
            where TInterface : class
        {
            services.AddRestweenRequestBuilding();

            services.AddScoped(provider =>
            {
                var requestBuilder = provider.GetRequiredService<IRestweenRequestBuilder>();
                return ApiClientFactory.CreateClient<TInterface>(httpClient, requestHandler, requestBuilder);
            });

            return services;
        }

        public static IServiceCollection AddApiClient<TApi, THandler>(
            this IServiceCollection services,
            Uri baseAddress)
            where TApi : class
            where THandler : class, IRequestHandler
        {
            services.AddRestweenRequestBuilding();

            services.AddScoped<TApi>(provider =>
            {
                var handler = provider.GetRequiredService<THandler>();
                var requestBuilder = provider.GetRequiredService<IRestweenRequestBuilder>();
                var httpClient = new HttpClient { BaseAddress = baseAddress };

                return ApiClientFactory.CreateClient<TApi>(httpClient, handler, requestBuilder);
            });

            return services;
        }
    }
}
