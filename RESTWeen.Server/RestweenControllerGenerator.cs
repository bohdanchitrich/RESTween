using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RESTween.Server;

[Generator]
public sealed class RestweenControllerGenerator : ISourceGenerator
{
    private const string GetAttributeName = "RESTween.Attributes.GetAttribute";
    private const string PostAttributeName = "RESTween.Attributes.PostAttribute";
    private const string PutAttributeName = "RESTween.Attributes.PutAttribute";
    private const string DeleteAttributeName = "RESTween.Attributes.DeleteAttribute";
    private const string MvcHttpGetAttributeName = "Microsoft.AspNetCore.Mvc.HttpGetAttribute";
    private const string MvcHttpPostAttributeName = "Microsoft.AspNetCore.Mvc.HttpPostAttribute";
    private const string MvcHttpPutAttributeName = "Microsoft.AspNetCore.Mvc.HttpPutAttribute";
    private const string MvcHttpDeleteAttributeName = "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute";
    private const string QueryAttributeName = "RESTween.Attributes.QueryAttribute";
    private const string RouteAttributeName = "RESTween.Attributes.RouteAttribute";
    private const string BodyAttributeName = "RESTween.Attributes.BodyAttribute";
    private const string HeaderAttributeName = "RESTween.Attributes.HeaderAttribute";
    private const string AllowAnonymousAttributeName = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";
    private const string AuthorizeAttributeName = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";

    private static readonly DiagnosticDescriptor AmbiguousHttpMethodDescriptor = new(
        "RESTWEEN001",
        "Ambiguous RESTween endpoint HTTP method",
        "Method '{0}' has multiple HTTP method attributes. Use either one RESTween HTTP attribute or one ASP.NET Core HTTP attribute.",
        "RESTween.Server",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(static () => new InterfaceSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not InterfaceSyntaxReceiver receiver)
            return;

        var interfaces = receiver.Candidates
            .Select(interfaceDeclaration =>
            {
                var semanticModel = context.Compilation.GetSemanticModel(interfaceDeclaration.SyntaxTree);
                return semanticModel.GetDeclaredSymbol(interfaceDeclaration, context.CancellationToken);
            })
            .Where(symbol => symbol is not null)
            .Cast<INamedTypeSymbol>()
            .Concat(GetReferencedInterfaces(context.Compilation))
            .GroupBy(symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .Select(group => group.First());

        foreach (var interfaceSymbol in interfaces)
        {
            if (!CanGenerateController(interfaceSymbol))
                continue;

            var controller = GenerateController(interfaceSymbol, context);
            if (controller is null)
                continue;

            context.AddSource($"{GetHintName(interfaceSymbol)}.RestweenController.g.cs", controller);
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetReferencedInterfaces(Compilation compilation)
    {
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (var interfaceSymbol in GetInterfaces(assembly.GlobalNamespace))
                yield return interfaceSymbol;
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetInterfaces(INamespaceSymbol namespaceSymbol)
    {
        foreach (var memberNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var interfaceSymbol in GetInterfaces(memberNamespace))
                yield return interfaceSymbol;
        }

        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var interfaceSymbol in GetInterfaces(type))
                yield return interfaceSymbol;
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetInterfaces(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
            yield return type;

        foreach (var nestedType in type.GetTypeMembers())
        {
            foreach (var interfaceSymbol in GetInterfaces(nestedType))
                yield return interfaceSymbol;
        }
    }

    private static bool CanGenerateController(INamedTypeSymbol interfaceSymbol)
    {
        if (interfaceSymbol.TypeKind != TypeKind.Interface)
            return false;

        return interfaceSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.MethodKind == MethodKind.Ordinary)
            .Any(method => method.GetAttributes().Any(attribute => GetHttpMethodInfo(attribute) is not null));
    }

    private static string? GenerateController(INamedTypeSymbol interfaceSymbol, GeneratorExecutionContext context)
    {
        var methods = interfaceSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.MethodKind == MethodKind.Ordinary)
            .Select(method => GetEndpointMethod(method, context))
            .Where(method => method is not null)
            .Cast<EndpointMethod>()
            .ToArray();

        if (methods.Length == 0)
            return null;

        var namespaceName = interfaceSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : interfaceSymbol.ContainingNamespace.ToDisplayString();

        var interfaceType = interfaceSymbol.ToDisplayString(TypeFormat);
        var controllerAttributes = GetPassthroughAttributes(interfaceSymbol.GetAttributes());
        var controllerName = GetControllerName(interfaceSymbol);
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (namespaceName is not null)
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine();
            builder.AppendLine("{");
        }

        var indent = namespaceName is null ? string.Empty : "    ";
        builder.Append(indent).AppendLine("[global::Microsoft.AspNetCore.Mvc.ApiController]");
        foreach (var attribute in controllerAttributes)
            builder.Append(indent).AppendLine(attribute);

        builder.Append(indent).Append("public sealed class ").Append(controllerName).AppendLine(" : global::Microsoft.AspNetCore.Mvc.ControllerBase");
        builder.Append(indent).AppendLine("{");
        builder.Append(indent).Append("    private readonly ").Append(interfaceType).AppendLine(" _handler;");
        builder.AppendLine();
        builder.Append(indent).Append("    public ").Append(controllerName).Append("(").Append(interfaceType).AppendLine(" handler)");
        builder.Append(indent).AppendLine("    {");
        builder.Append(indent).AppendLine("        _handler = handler ?? throw new global::System.ArgumentNullException(nameof(handler));");
        builder.Append(indent).AppendLine("    }");

        foreach (var method in methods)
        {
            builder.AppendLine();
            foreach (var attribute in method.PassthroughAttributes)
                builder.Append(indent).Append("    ").AppendLine(attribute);

            builder.Append(indent).Append("    [global::Microsoft.AspNetCore.Mvc.")
                .Append(method.MvcHttpAttribute)
                .Append(FormatMvcRouteArgument(method.Url))
                .AppendLine("]");

            builder.Append(indent)
                .Append("    public ")
                .Append(method.ReturnType)
                .Append(' ')
                .Append(method.Name)
                .Append('(')
                .Append(string.Join(", ", method.Parameters.Select(parameter => parameter.Declaration)))
                .AppendLine(")");

            if (method.ReturnsVoid)
            {
                builder.Append(indent).AppendLine("    {");
                builder.Append(indent).Append("        _handler.").Append(method.Name).Append('(')
                    .Append(string.Join(", ", method.Parameters.Select(parameter => parameter.Name)))
                    .AppendLine(");");
                builder.Append(indent).AppendLine("    }");
            }
            else
            {
                builder.Append(indent).Append("        => _handler.").Append(method.Name).Append('(')
                    .Append(string.Join(", ", method.Parameters.Select(parameter => parameter.Name)))
                    .AppendLine(");");
            }
        }

        builder.Append(indent).AppendLine("}");

        if (namespaceName is not null)
            builder.AppendLine("}");

        return builder.ToString();
    }

    private static EndpointMethod? GetEndpointMethod(IMethodSymbol method, GeneratorExecutionContext context)
    {
        var http = GetHttpMethod(method, context);
        if (http is null)
            return null;

        var parameters = method.Parameters
            .Select(parameter => GetParameter(method, parameter, http.Value.Url, http.Value.IsBodyMethod))
            .ToArray();

        return new EndpointMethod(
            method.Name,
            method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(TypeFormat),
            method.ReturnsVoid,
            http.Value.MvcHttpAttribute,
            http.Value.Url,
            GetPassthroughAttributes(method.GetAttributes()),
            parameters);
    }

    private static HttpMethodInfo? GetHttpMethod(IMethodSymbol method, GeneratorExecutionContext context)
    {
        var httpAttributes = method.GetAttributes()
            .Select(GetHttpMethodInfo)
            .Where(info => info is not null)
            .Cast<HttpMethodInfo>()
            .ToArray();

        if (httpAttributes.Length == 0)
            return null;

        if (httpAttributes.Length > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AmbiguousHttpMethodDescriptor,
                method.Locations.FirstOrDefault(),
                method.Name));
            return null;
        }

        return httpAttributes[0];
    }

    private static HttpMethodInfo? GetHttpMethodInfo(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.ToDisplayString();
        var url = GetFirstStringConstructorArgument(attribute);

        return name switch
        {
            GetAttributeName when url is not null => new HttpMethodInfo("HttpGet", url, false),
            PostAttributeName when url is not null => new HttpMethodInfo("HttpPost", url, true),
            PutAttributeName when url is not null => new HttpMethodInfo("HttpPut", url, true),
            DeleteAttributeName when url is not null => new HttpMethodInfo("HttpDelete", url, false),
            MvcHttpGetAttributeName => new HttpMethodInfo("HttpGet", url, false),
            MvcHttpPostAttributeName => new HttpMethodInfo("HttpPost", url, true),
            MvcHttpPutAttributeName => new HttpMethodInfo("HttpPut", url, true),
            MvcHttpDeleteAttributeName => new HttpMethodInfo("HttpDelete", url, false),
            _ => null
        };
    }

    private static GeneratedParameter GetParameter(IMethodSymbol method, IParameterSymbol parameter, string? url, bool isBodyMethod)
    {
        var attribute = GetBindingAttribute(method, parameter, url, isBodyMethod);
        var type = parameter.Type.ToDisplayString(TypeFormat);
        var name = parameter.Name;
        return new GeneratedParameter(name, $"{attribute} {type} {name}");
    }

    private static string GetBindingAttribute(IMethodSymbol method, IParameterSymbol parameter, string? url, bool isBodyMethod)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName == RouteAttributeName)
                return BuildMvcBindingAttribute("FromRoute", GetFirstStringConstructorArgument(attribute) ?? parameter.Name);

            if (attributeName == QueryAttributeName)
                return BuildMvcBindingAttribute("FromQuery", GetFirstStringConstructorArgument(attribute) ?? parameter.Name);

            if (attributeName == BodyAttributeName)
                return "[global::Microsoft.AspNetCore.Mvc.FromBody]";

            if (attributeName == HeaderAttributeName)
                return BuildMvcBindingAttribute("FromHeader", GetFirstStringConstructorArgument(attribute) ?? parameter.Name);
        }

        if (url?.IndexOf("{" + parameter.Name + "}", StringComparison.Ordinal) >= 0)
            return BuildMvcBindingAttribute("FromRoute", parameter.Name);

        if (IsSimpleType(parameter.Type))
            return BuildMvcBindingAttribute("FromQuery", parameter.Name);

        if (isBodyMethod)
            return "[global::Microsoft.AspNetCore.Mvc.FromBody]";

        return BuildMvcBindingAttribute("FromQuery", parameter.Name);
    }

    private static bool IsSimpleType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            return IsSimpleType(namedType.TypeArguments[0]);

        if (type.TypeKind == TypeKind.Enum)
            return true;

        if (type.SpecialType is SpecialType.System_Boolean
            or SpecialType.System_Byte
            or SpecialType.System_Char
            or SpecialType.System_DateTime
            or SpecialType.System_Decimal
            or SpecialType.System_Double
            or SpecialType.System_Int16
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_SByte
            or SpecialType.System_Single
            or SpecialType.System_String
            or SpecialType.System_UInt16
            or SpecialType.System_UInt32
            or SpecialType.System_UInt64)
        {
            return true;
        }

        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Guid";
    }

    private static string BuildMvcBindingAttribute(string attributeName, string name)
    {
        return $"[global::Microsoft.AspNetCore.Mvc.{attributeName}(Name = \"{EscapeString(name)}\")]";
    }

    private static IReadOnlyList<string> GetPassthroughAttributes(ImmutableArray<AttributeData> attributes)
    {
        return attributes
            .Select(GetPassthroughAttribute)
            .Where(attribute => attribute is not null)
            .Cast<string>()
            .ToArray();
    }

    private static string? GetPassthroughAttribute(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.ToDisplayString();
        return name switch
        {
            AllowAnonymousAttributeName => "[global::Microsoft.AspNetCore.Authorization.AllowAnonymous]",
            AuthorizeAttributeName => BuildAuthorizeAttribute(attribute),
            _ => null
        };
    }

    private static string BuildAuthorizeAttribute(AttributeData attribute)
    {
        var arguments = new List<string>();

        if (attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string policy
            && !string.IsNullOrWhiteSpace(policy))
        {
            arguments.Add($"\"{EscapeString(policy)}\"");
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Value.Value is string value && !string.IsNullOrWhiteSpace(value))
                arguments.Add($"{namedArgument.Key} = \"{EscapeString(value)}\"");
        }

        return arguments.Count == 0
            ? "[global::Microsoft.AspNetCore.Authorization.Authorize]"
            : "[global::Microsoft.AspNetCore.Authorization.Authorize(" + string.Join(", ", arguments) + ")]";
    }

    private static string? GetFirstStringConstructorArgument(AttributeData attribute)
    {
        return attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string value
            && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
    }

    private static string FormatMvcRouteArgument(string? url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : $"(\"{EscapeString(url!)}\")";
    }

    private static string GetControllerName(INamedTypeSymbol interfaceSymbol)
    {
        var name = interfaceSymbol.Name;
        if (name.Length > 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);

        return name + "Controller";
    }

    private static string GetHintName(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.ToDisplayString()
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('.', '_')
            .Replace(',', '_')
            .Replace(' ', '_');
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private sealed class InterfaceSyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> Candidates { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclaration
                && HasAttributes(interfaceDeclaration))
            {
                Candidates.Add(interfaceDeclaration);
            }
        }

        private static bool HasAttributes(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            if (interfaceDeclaration.AttributeLists.Count > 0)
                return true;

            return interfaceDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(method => method.AttributeLists.Count > 0);
        }
    }

    private readonly struct HttpMethodInfo
    {
        public HttpMethodInfo(string mvcHttpAttribute, string? url, bool isBodyMethod)
        {
            MvcHttpAttribute = mvcHttpAttribute;
            Url = url;
            IsBodyMethod = isBodyMethod;
        }

        public string MvcHttpAttribute { get; }
        public string? Url { get; }
        public bool IsBodyMethod { get; }
    }

    private sealed class EndpointMethod
    {
        public EndpointMethod(
            string name,
            string returnType,
            bool returnsVoid,
            string mvcHttpAttribute,
            string? url,
            IReadOnlyList<string> passthroughAttributes,
            IReadOnlyList<GeneratedParameter> parameters)
        {
            Name = name;
            ReturnType = returnType;
            ReturnsVoid = returnsVoid;
            MvcHttpAttribute = mvcHttpAttribute;
            Url = url;
            PassthroughAttributes = passthroughAttributes;
            Parameters = parameters;
        }

        public string Name { get; }
        public string ReturnType { get; }
        public bool ReturnsVoid { get; }
        public string MvcHttpAttribute { get; }
        public string? Url { get; }

        public IReadOnlyList<string> PassthroughAttributes { get; }
        public IReadOnlyList<GeneratedParameter> Parameters { get; }
    }

    private sealed class GeneratedParameter
    {
        public GeneratedParameter(string name, string declaration)
        {
            Name = name;
            Declaration = declaration;
        }

        public string Name { get; }
        public string Declaration { get; }
    }
}
