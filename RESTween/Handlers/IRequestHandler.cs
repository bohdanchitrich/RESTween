using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RESTween.Handlers
{
    public interface IRequestHandler
    {
        public abstract Task<T> HandleRequestAsync<T>(HttpRequestMessage request, HttpClient httpClient);
        public abstract Task HandleRequestAsync(HttpRequestMessage request, HttpClient httpClient);

    }

}
