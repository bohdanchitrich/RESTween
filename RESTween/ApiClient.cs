using Castle.DynamicProxy;
using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace RESTween
{
    public class ApiClient
    {
        private readonly IRequestHandler _requestHandler;
        private readonly HttpClient _httpClient;

        public ApiClient(IRequestHandler requestHandler, HttpClient httpClient)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _httpClient = httpClient;
        }

        public async Task<T> CallAsync<T>(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameterInfos, parameters);
            return await _requestHandler.HandleRequestAsync<T>(request, _httpClient);
        }

        public async Task CallAsync(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameterInfos, parameters);
            await _requestHandler.HandleRequestAsync(request, _httpClient);
        }

        public HttpRequestMessage CreateRequest(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage request = null;

            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
            {
                request = HandleGet(getAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
            {
                request = HandlePost(postAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
            {
                request = HandlePut(putAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
            {
                request = HandleDelete(deleteAttr.Url, parameterInfos, parameters);
            }
            else
            {
                throw new NotImplementedException("Only GET, POST, PUT, and DELETE methods are supported.");
            }

            return request;
        }
        private HttpRequestMessage HandleGet(string url, ParameterInfo[] parameterInfos, object[] parametersValues)
        {

            object body = null;
            var quarries = new Dictionary<string, object>();
            var routes = new Dictionary<string, object>();

            for (int i = 0; i < parametersValues.Length; i++)
            {
                var value = parametersValues[i];
                var info = parameterInfos[i];
                var paramName = info.Name;
                if (paramName == null) continue;

                //Parameter don`t have attributes
                if (!HasAttributes(info))
                {

                    //Parameter not simple type
                    if (!ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        if (body != null)
                        {
                            throw new Exception($"Request {url} can have only one body parameter");
                        }
                        body = value;
                        continue;
                    }
                    //Parameter in route
                    if (url.Contains($"{{{paramName}}}"))
                    {
                        //Parameter already exist
                        if (routes.ContainsKey(paramName))
                        {
                            throw new Exception($"{paramName} duplicated in {url}");
                        }
                        routes[paramName] = value;
                        continue;
                    }
                    //Parameter in query

                    //Parameter already exist
                    if (quarries.ContainsKey(paramName))
                    {
                        throw new Exception($"{paramName} duplicated int {url}");
                    }
                    quarries[paramName] = value;
                    continue;
                }
                //Parameter have attribute route
                var routeAttribute = info.GetCustomAttribute<RouteAttribute>();
                var routeName = routeAttribute?.Name ?? paramName;
                if (routeAttribute != null)
                {
                    if (!url.Contains($"{{{routeName}}}"))
                    {
                        throw new Exception($"Route {url} not contain {routeName} parameter");
                    }
                    //Parameter already exist
                    if (routes.ContainsKey(paramName))
                    {
                        throw new Exception($"{paramName} duplicated in {url}");
                    }
                    routes[paramName] = value;
                    continue;
                }
                //Parameter have attribute query
                var queryAttribute = info.GetCustomAttribute<QueryAttribute>();
                var queryName = queryAttribute?.Name ?? paramName;
                if (queryAttribute != null)
                {
                    //Parameter already exist
                    if (quarries.ContainsKey(queryName))
                    {
                        throw new Exception($"{queryName} duplicated in {url}");
                    }
                    quarries[queryName] = value;
                    continue;
                }

                var bodyAttribute = info.GetCustomAttribute<BodyAttribute>();
                if (!ParameterTypeChecker.IsSimpleType(info.ParameterType))
                {
                    if (body != null)
                    {
                        throw new Exception($"Request {url} can have only one body parameter");
                    }
                    body = value;
                    continue;
                }


                throw new Exception($"Fail to parse parameter {paramName} in {url}");
            }
            //Apply route parameters to url
            foreach (var routeParam in routes)
            {

                url = url.Replace($"{{{routeParam.Key}}}", HttpUtility.UrlEncode(routeParam.Value?.ToString()!));
            }
            //Creating quary segment
            var query = string.Join("&", quarries
    .Where(keyPair => keyPair.Value?.ToString() != null)
    .Select(keyPair => $"{keyPair.Key}={HttpUtility.UrlEncode(keyPair.Value?.ToString()!)}"));


            //Apply query parameters to url
            if (!string.IsNullOrEmpty(query))
            {

                url = $"{url}?{query}";
            }
            //Apply body parameter 
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Content = body != null ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") : null
            };

            return httpRequestMessage;
        }


        private HttpRequestMessage HandlePost(string url, ParameterInfo[] parameterInfos, object[] parametersValues)
        {
            object body = null;
            var quarries = new Dictionary<string, object>();
            var routes = new Dictionary<string, object>();

            for (int i = 0; i < parametersValues.Length; i++)
            {
                var value = parametersValues[i];
                var info = parameterInfos[i];
                var paramName = info.Name;
                if (paramName == null) continue;

                //Parameter don`t have attributes
                if (!HasAttributes(info))
                {

                    //Parameter not simple type
                    if (!ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        if (body != null)
                        {
                            throw new Exception($"Request {url} can have only one body parameter");
                        }
                        body = value;
                        continue;
                    }
                    //Parameter in route
                    if (url.Contains($"{{{paramName}}}"))
                    {
                        //Parameter already exist
                        if (routes.ContainsKey(paramName))
                        {
                            throw new Exception($"{paramName} duplicated in {url}");
                        }
                        routes[paramName] = value;
                        continue;
                    }
                    //Parameter in query

                    //Parameter already exist
                    if (quarries.ContainsKey(paramName))
                    {
                        throw new Exception($"{paramName} duplicated int {url}");
                    }
                    quarries[paramName] = value;
                    continue;
                }
                //Parameter have attribute route
                var routeAttribute = info.GetCustomAttribute<RouteAttribute>();
                var routeName = routeAttribute?.Name ?? paramName;
                if (routeAttribute != null)
                {
                    if (!url.Contains($"{{{routeName}}}"))
                    {
                        throw new Exception($"Route {url} not contain {routeName} parameter");
                    }
                    //Parameter already exist
                    if (routes.ContainsKey(paramName))
                    {
                        throw new Exception($"{paramName} duplicated in {url}");
                    }
                    routes[paramName] = value;
                    continue;
                }
                //Parameter have attribute query
                var queryAttribute = info.GetCustomAttribute<QueryAttribute>();
                var queryName = queryAttribute?.Name ?? paramName;
                if (queryAttribute != null)
                {
                    //Parameter already exist
                    if (quarries.ContainsKey(queryName))
                    {
                        throw new Exception($"{queryName} duplicated in {url}");
                    }
                    quarries[queryName] = value;
                    continue;
                }

                var bodyAttribute = info.GetCustomAttribute<BodyAttribute>();
                if (bodyAttribute != null)
                {
                    if (body != null)
                    {
                        throw new Exception($"Request {url} can have only one body parameter");
                    }
                    body = value;
                    continue;
                }


                throw new Exception($"Fail to parse parameter {paramName} in {url}");
            }
            //Apply route parameters to url
            foreach (var routeParam in routes)
            {

                url = url.Replace($"{{{routeParam.Key}}}", HttpUtility.UrlEncode(routeParam.Value?.ToString()!));
            }
            //Creating quary segment
            var query = string.Join("&", quarries
.Where(keyPair => keyPair.Value?.ToString() != null)
.Select(keyPair => $"{keyPair.Key}={HttpUtility.UrlEncode(keyPair.Value?.ToString()!)}"));


            //Apply query parameters to url
            if (!string.IsNullOrEmpty(query))
            {
                url = $"{url}?{query}";
            }
            //Apply body parameter 
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = body != null ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") : null
            };

            return httpRequestMessage;
        }





        private HttpRequestMessage HandlePut(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var result = HandlePost(url, parameterInfos, parameters);
            result.Method = HttpMethod.Put;
            return result;
        }

        private HttpRequestMessage HandleDelete(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var result = HandleGet(url, parameterInfos, parameters);
            result.Method = HttpMethod.Delete;
            return result;
        }




        private bool HasAttributes(ParameterInfo parameterInfo)
        {
            var attributes = parameterInfo.GetCustomAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute.GetType() == typeof(QueryAttribute)) return true;
                if (attribute.GetType() == typeof(RouteAttribute)) return true;
                if (attribute.GetType() == typeof(BodyAttribute)) return true;

            }
            return false;
        }



    }

}


