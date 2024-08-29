using Microsoft.Extensions.DependencyInjection;
using RESTween.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

    }
}
