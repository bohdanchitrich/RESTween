using Microsoft.AspNetCore.Http;
using RESTween.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Server
{
    internal class ApiDispatcher
    {
        private readonly object _implementation;
        private readonly List<Endpoint> _endpoints = new();

        private ApiDispatcher(object implementation)
        {
            _implementation = implementation;
        }

        public static ApiDispatcher Create<TInterface>(TInterface implementation)
            where TInterface : class
        {
            var dispatcher = new ApiDispatcher(implementation);
            dispatcher.Initialize(typeof(TInterface));
            return dispatcher;
        }

        private void Initialize(Type interfaceType)
        {
            foreach (var method in interfaceType.GetMethods())
            {
                if (TryParseVerbAndUrl(method, out var verb, out var url))
                {
                    _endpoints.Add(new Endpoint
                    {
                        Verb = verb,
                        Url = url.Trim('/'),
                        Method = method
                    });
                }
            }
        }

        private static bool TryParseVerbAndUrl(MethodInfo method, out string verb, out string url)
        {
            verb = null!;
            url = null!;
            if (method.GetCustomAttribute<GetAttribute>() is { } g)
            {
                verb = "GET"; url = g.Url; return true;
            }
            if (method.GetCustomAttribute<PostAttribute>() is { } p)
            {
                verb = "POST"; url = p.Url; return true;
            }
            if (method.GetCustomAttribute<PutAttribute>() is { } u)
            {
                verb = "PUT"; url = u.Url; return true;
            }
            if (method.GetCustomAttribute<DeleteAttribute>() is { } d)
            {
                verb = "DELETE"; url = d.Url; return true;
            }
            return false;
        }

        private record Endpoint
        {
            public string Verb { get; init; } = "";
            public string Url { get; init; } = "";
            public MethodInfo Method { get; init; } = null!;
        };

        public async Task<bool> Handle(HttpContext context)
        {
            var reqMethod = context.Request.Method.ToUpperInvariant();
            var reqPath = context.Request.Path.Value?.Trim('/') ?? "";

            var endpoint = _endpoints.FirstOrDefault(e =>
                e.Verb == reqMethod &&
                string.Equals(e.Url, reqPath, StringComparison.OrdinalIgnoreCase));

            if (endpoint == null)
                return false;

            // --- розбір аргументів (Body + Query) ---
            var args = await BindParameters(context, endpoint.Method);

            // --- виклик методу ---
            var taskObj = (Task?)endpoint.Method.Invoke(_implementation, args);
            if (taskObj == null)
                return true;

            await taskObj.ConfigureAwait(false);

            var resultProp = taskObj.GetType().GetProperty("Result");
            var result = resultProp?.GetValue(taskObj);

            context.Response.ContentType = "application/json";
           
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);

            return true;
        }

        private async Task<object?[]> BindParameters(HttpContext ctx, MethodInfo method)
        {
            var ps = method.GetParameters();
            var args = new object?[ps.Length];

            // Пошук параметра тіла (Body)
            var bodyParam = ps.FirstOrDefault(p =>
                p.GetCustomAttribute<BodyAttribute>() != null ||
                (!p.ParameterType.IsPrimitive && p.ParameterType != typeof(string)));

            object? body = null;
            if (bodyParam != null && ctx.Request.ContentLength > 0)
            {
                body = await System.Text.Json.JsonSerializer.DeserializeAsync(
                    ctx.Request.Body, bodyParam.ParameterType,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (p == bodyParam)
                {
                    args[i] = body;
                    continue;
                }

                var q = ctx.Request.Query[p.Name!].FirstOrDefault();
                args[i] = ConvertSimple(q, p.ParameterType);
            }

            return args;
        }

        private static object? ConvertSimple(string? raw, Type t)
        {
            if (raw == null) return null;
            var ut = Nullable.GetUnderlyingType(t) ?? t;
            if (ut == typeof(string)) return raw;
            if (ut.IsEnum) return Enum.Parse(ut, raw, true);
            if (ut == typeof(Guid)) return Guid.Parse(raw);
            if (ut == typeof(int)) return int.Parse(raw);
            if (ut == typeof(bool)) return bool.Parse(raw);
            return Convert.ChangeType(raw, ut);
        }
    }

}
