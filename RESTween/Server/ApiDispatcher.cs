using Microsoft.AspNetCore.Http;
using RESTween.Core;
using RESTween.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RESTween.Server
{
    internal class ApiDispatcher
    {
        private readonly object _implementation;
        private readonly Dictionary<Guid, MethodInfo> _methodsById = new();

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
               
                    var id = MethodIdGenerator.Create(method);
                    _methodsById[id] = method;
            }
        }

      

        public async Task<bool> Handle(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("X-RT-MethodId", out var header) ||
                !Guid.TryParse(header, out var methodId))
                return false;

            if (!_methodsById.TryGetValue(methodId, out var method))
                return false;

            var args = await BindParameters(context, method);

            var taskObj = (Task?)method.Invoke(_implementation, args);
            if (taskObj == null) return true;

            await taskObj.ConfigureAwait(false);
            var resultProp = taskObj.GetType().GetProperty("Result");
            var result = resultProp?.GetValue(taskObj);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            return true;
        }

        private async Task<object?[]> BindParameters(HttpContext ctx, MethodInfo method)
        {
            var ps = method.GetParameters();
            var args = new object?[ps.Length];

            var bodyParam = ps.FirstOrDefault(p =>
                p.GetCustomAttribute<BodyAttribute>() != null ||
                (!p.ParameterType.IsPrimitive && p.ParameterType != typeof(string)));

            object? body = null;
            if (bodyParam != null && ctx.Request.ContentLength > 0)
            {
                body = await JsonSerializer.DeserializeAsync(
                    ctx.Request.Body, bodyParam.ParameterType,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
