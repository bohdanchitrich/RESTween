using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using RESTween.Attributes;
using ApiRouteAttribute = RESTween.Attributes.RouteAttribute;

namespace RESTween.Server;

internal static class RuntimeControllerAssemblyBuilder
{
    private const string RuntimeNamespace = "RESTween.RuntimeGenerated";

    public static Assembly BuildAssembly(IReadOnlyCollection<Type> apiInterfaces)
    {
        if (apiInterfaces is null)
            throw new ArgumentNullException(nameof(apiInterfaces));

        var assemblyName = new AssemblyName("RuntimeControllers." + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("RuntimeControllers");
        var generatedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var apiInterface in apiInterfaces)
        {
            ValidateApiInterface(apiInterface);

            if (!apiInterface.IsDefined(typeof(RestweenControllerAttribute), inherit: false))
                continue;

            var methods = apiInterface
                .GetMethods()
                .Select(CreateEndpointMethod)
                .Where(method => method is not null)
                .Cast<EndpointMethod>()
                .ToArray();

            if (methods.Length == 0)
                continue;

            DefineControllerType(moduleBuilder, apiInterface, methods, generatedNames);
        }

        return assemblyBuilder;
    }

    private static void ValidateApiInterface(Type apiInterface)
    {
        if (apiInterface is null)
            throw new ArgumentNullException(nameof(apiInterface));

        if (!apiInterface.IsInterface)
            throw new ArgumentException("Runtime controllers can only be generated for interface types.", nameof(apiInterface));
    }

    private static void DefineControllerType(
        ModuleBuilder moduleBuilder,
        Type apiInterface,
        IReadOnlyList<EndpointMethod> methods,
        ISet<string> generatedNames)
    {
        var controllerName = GetControllerName(apiInterface);
        var typeName = RuntimeNamespace + "." + controllerName;

        if (!generatedNames.Add(typeName))
            throw new InvalidOperationException($"A runtime controller named '{controllerName}' has already been generated. Use unique API interface names.");

        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
            typeof(ControllerBase));

        typeBuilder.SetCustomAttribute(CreateParameterlessAttribute(typeof(ApiControllerAttribute)));

        var handlerField = typeBuilder.DefineField(
            "_handler",
            apiInterface,
            FieldAttributes.Private | FieldAttributes.InitOnly);

        DefineConstructor(typeBuilder, apiInterface, handlerField);

        foreach (var method in methods)
            DefineActionMethod(typeBuilder, handlerField, method);

        typeBuilder.CreateTypeInfo();
    }

    private static void DefineConstructor(TypeBuilder typeBuilder, Type apiInterface, FieldBuilder handlerField)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { apiInterface });

        var baseConstructor = typeof(ControllerBase).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        if (baseConstructor is null)
            throw new InvalidOperationException("Could not find the ControllerBase parameterless constructor.");

        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, baseConstructor);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, handlerField);
        il.Emit(OpCodes.Ret);
    }

    private static void DefineActionMethod(
        TypeBuilder typeBuilder,
        FieldBuilder handlerField,
        EndpointMethod endpoint)
    {
        var parameterTypes = endpoint.Parameters.Select(parameter => parameter.ParameterType).ToArray();
        var methodBuilder = typeBuilder.DefineMethod(
            endpoint.InterfaceMethod.Name,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
            endpoint.InterfaceMethod.ReturnType,
            parameterTypes);

        foreach (var attribute in endpoint.PassthroughAttributes)
            methodBuilder.SetCustomAttribute(attribute);

        methodBuilder.SetCustomAttribute(CreateHttpAttribute(endpoint.HttpAttributeType, endpoint.Url));

        for (var i = 0; i < endpoint.Parameters.Count; i++)
        {
            var parameter = endpoint.Parameters[i];
            var parameterBuilder = methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameter.Name);
            parameterBuilder.SetCustomAttribute(parameter.BindingAttribute);
        }

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, handlerField);

        for (var i = 0; i < endpoint.Parameters.Count; i++)
            EmitLoadArgument(il, i + 1);

        il.Emit(OpCodes.Callvirt, endpoint.InterfaceMethod);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitLoadArgument(ILGenerator il, int index)
    {
        switch (index)
        {
            case 0:
                il.Emit(OpCodes.Ldarg_0);
                break;
            case 1:
                il.Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                il.Emit(OpCodes.Ldarg_2);
                break;
            case 3:
                il.Emit(OpCodes.Ldarg_3);
                break;
            default:
                il.Emit(OpCodes.Ldarg_S, (short)index);
                break;
        }
    }

    private static EndpointMethod? CreateEndpointMethod(MethodInfo method)
    {
        var httpAttributes = method
            .GetCustomAttributes(inherit: false)
            .Select(GetHttpMethodInfo)
            .Where(info => info is not null)
            .Cast<HttpMethodInfo>()
            .ToArray();

        if (httpAttributes.Length == 0)
            return null;

        if (httpAttributes.Length > 1)
            throw new InvalidOperationException($"Method '{method.DeclaringType?.FullName}.{method.Name}' has multiple HTTP method attributes.");

        var http = httpAttributes[0];
        var parameters = method
            .GetParameters()
            .Select(parameter => CreateEndpointParameter(parameter, http.Url, http.IsBodyMethod))
            .ToArray();

        return new EndpointMethod(
            method,
            http.MvcAttributeType,
            http.Url,
            GetPassthroughAttributes(method),
            parameters);
    }

    private static HttpMethodInfo? GetHttpMethodInfo(object attribute)
    {
        return attribute switch
        {
            GetAttribute api => new HttpMethodInfo(typeof(HttpGetAttribute), api.Url, false),
            PostAttribute api => new HttpMethodInfo(typeof(HttpPostAttribute), api.Url, true),
            PutAttribute api => new HttpMethodInfo(typeof(HttpPutAttribute), api.Url, true),
            DeleteAttribute api => new HttpMethodInfo(typeof(HttpDeleteAttribute), api.Url, false),
            HttpGetAttribute mvc => new HttpMethodInfo(typeof(HttpGetAttribute), mvc.Template, false),
            HttpPostAttribute mvc => new HttpMethodInfo(typeof(HttpPostAttribute), mvc.Template, true),
            HttpPutAttribute mvc => new HttpMethodInfo(typeof(HttpPutAttribute), mvc.Template, true),
            HttpDeleteAttribute mvc => new HttpMethodInfo(typeof(HttpDeleteAttribute), mvc.Template, false),
            _ => null
        };
    }

    private static EndpointParameter CreateEndpointParameter(ParameterInfo parameter, string? url, bool isBodyMethod)
    {
        return new EndpointParameter(
            parameter.Name ?? "parameter",
            parameter.ParameterType,
            GetBindingAttribute(parameter, url, isBodyMethod));
    }

    private static CustomAttributeBuilder GetBindingAttribute(ParameterInfo parameter, string? url, bool isBodyMethod)
    {
        var routeAttribute = parameter.GetCustomAttribute<ApiRouteAttribute>();
        if (routeAttribute is not null)
            return CreateNamedBindingAttribute(typeof(FromRouteAttribute), routeAttribute.Name ?? parameter.Name!);

        var queryAttribute = parameter.GetCustomAttribute<QueryAttribute>();
        if (queryAttribute is not null)
            return CreateNamedBindingAttribute(typeof(FromQueryAttribute), queryAttribute.Name ?? parameter.Name!);

        if (parameter.IsDefined(typeof(BodyAttribute), inherit: false))
            return CreateParameterlessAttribute(typeof(FromBodyAttribute));

        var headerAttribute = parameter.GetCustomAttribute<HeaderAttribute>();
        if (headerAttribute is not null)
            return CreateNamedBindingAttribute(typeof(FromHeaderAttribute), headerAttribute.Name ?? parameter.Name!);

        if (url?.IndexOf("{" + parameter.Name + "}", StringComparison.Ordinal) >= 0)
            return CreateNamedBindingAttribute(typeof(FromRouteAttribute), parameter.Name!);

        if (IsSimpleType(parameter.ParameterType))
            return CreateNamedBindingAttribute(typeof(FromQueryAttribute), parameter.Name!);

        if (isBodyMethod)
            return CreateParameterlessAttribute(typeof(FromBodyAttribute));

        return CreateNamedBindingAttribute(typeof(FromQueryAttribute), parameter.Name!);
    }

    private static IReadOnlyList<CustomAttributeBuilder> GetPassthroughAttributes(MethodInfo method)
    {
        var attributes = new List<CustomAttributeBuilder>();

        if (method.IsDefined(typeof(AllowAnonymousAttribute), inherit: false))
            attributes.Add(CreateParameterlessAttribute(typeof(AllowAnonymousAttribute)));

        foreach (var authorize in method.GetCustomAttributes<AuthorizeAttribute>(inherit: false))
            attributes.Add(CreateAuthorizeAttribute(authorize));

        return attributes;
    }

    private static CustomAttributeBuilder CreateAuthorizeAttribute(AuthorizeAttribute authorize)
    {
        ConstructorInfo constructor;
        object[] constructorArguments;
        var namedProperties = new List<PropertyInfo>();
        var namedValues = new List<object>();

        if (!string.IsNullOrWhiteSpace(authorize.Policy))
        {
            constructor = typeof(AuthorizeAttribute).GetConstructor(new[] { typeof(string) })
                ?? throw new InvalidOperationException("Could not find AuthorizeAttribute(string) constructor.");
            constructorArguments = new object[] { authorize.Policy! };
        }
        else
        {
            constructor = typeof(AuthorizeAttribute).GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException("Could not find AuthorizeAttribute parameterless constructor.");
            constructorArguments = Array.Empty<object>();
        }

        AddNamedStringProperty(typeof(AuthorizeAttribute), namedProperties, namedValues, nameof(AuthorizeAttribute.Roles), authorize.Roles);
        AddNamedStringProperty(typeof(AuthorizeAttribute), namedProperties, namedValues, nameof(AuthorizeAttribute.AuthenticationSchemes), authorize.AuthenticationSchemes);

        return new CustomAttributeBuilder(
            constructor,
            constructorArguments,
            namedProperties.ToArray(),
            namedValues.ToArray());
    }

    private static void AddNamedStringProperty(
        Type attributeType,
        ICollection<PropertyInfo> properties,
        ICollection<object> values,
        string propertyName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var property = attributeType.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Could not find property '{propertyName}' on '{attributeType.FullName}'.");

        properties.Add(property);
        values.Add(value);
    }

    private static CustomAttributeBuilder CreateHttpAttribute(Type attributeType, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return CreateParameterlessAttribute(attributeType);

        var constructor = attributeType.GetConstructor(new[] { typeof(string) })
            ?? throw new InvalidOperationException($"Could not find {attributeType.FullName}(string) constructor.");

        return new CustomAttributeBuilder(constructor, new object[] { template! });
    }

    private static CustomAttributeBuilder CreateNamedBindingAttribute(Type attributeType, string name)
    {
        var constructor = attributeType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Could not find {attributeType.FullName} parameterless constructor.");
        var nameProperty = attributeType.GetProperty("Name")
            ?? throw new InvalidOperationException($"Could not find property 'Name' on '{attributeType.FullName}'.");

        return new CustomAttributeBuilder(
            constructor,
            Array.Empty<object>(),
            new[] { nameProperty },
            new object[] { name });
    }

    private static CustomAttributeBuilder CreateParameterlessAttribute(Type attributeType)
    {
        var constructor = attributeType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Could not find {attributeType.FullName} parameterless constructor.");

        return new CustomAttributeBuilder(constructor, Array.Empty<object>());
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return true;

        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(Guid);
    }

    private static string GetControllerName(Type apiInterface)
    {
        var name = apiInterface.Name;
        if (name.Length > 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);

        return name + "Controller";
    }

    private readonly struct HttpMethodInfo
    {
        public HttpMethodInfo(Type mvcAttributeType, string? url, bool isBodyMethod)
        {
            MvcAttributeType = mvcAttributeType;
            Url = url;
            IsBodyMethod = isBodyMethod;
        }

        public Type MvcAttributeType { get; }
        public string? Url { get; }
        public bool IsBodyMethod { get; }
    }

    private sealed class EndpointMethod
    {
        public EndpointMethod(
            MethodInfo interfaceMethod,
            Type httpAttributeType,
            string? url,
            IReadOnlyList<CustomAttributeBuilder> passthroughAttributes,
            IReadOnlyList<EndpointParameter> parameters)
        {
            InterfaceMethod = interfaceMethod;
            HttpAttributeType = httpAttributeType;
            Url = url;
            PassthroughAttributes = passthroughAttributes;
            Parameters = parameters;
        }

        public MethodInfo InterfaceMethod { get; }
        public Type HttpAttributeType { get; }
        public string? Url { get; }
        public IReadOnlyList<CustomAttributeBuilder> PassthroughAttributes { get; }
        public IReadOnlyList<EndpointParameter> Parameters { get; }
    }

    private sealed class EndpointParameter
    {
        public EndpointParameter(string name, Type parameterType, CustomAttributeBuilder bindingAttribute)
        {
            Name = name;
            ParameterType = parameterType;
            BindingAttribute = bindingAttribute;
        }

        public string Name { get; }
        public Type ParameterType { get; }
        public CustomAttributeBuilder BindingAttribute { get; }
    }
}
