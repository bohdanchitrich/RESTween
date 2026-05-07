using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RESTween.Attributes;
using RESTween.Server;
using ApiRouteAttribute = RESTween.Attributes.RouteAttribute;

namespace RESTween.Server.Tests;

[TestFixture]
public sealed class RuntimeControllerAssemblyBuilderTests
{
    [Test]
    public void BuildAssembly_GeneratesControllerMetadataAndDelegatesToHandler()
    {
        var assembly = RuntimeControllerAssemblyBuilder.BuildAssembly(new[] { typeof(IUserApi) });
        var controllerType = assembly.GetTypes().Single(type => type.Name == "UserApiController");

        Assert.That(controllerType.Namespace, Is.EqualTo("RESTween.RuntimeGenerated"));
        Assert.That(controllerType.BaseType, Is.EqualTo(typeof(ControllerBase)));
        Assert.That(controllerType.GetCustomAttribute<ApiControllerAttribute>(), Is.Not.Null);
        Assert.That(controllerType.GetConstructor(new[] { typeof(IUserApi) }), Is.Not.Null);

        var method = controllerType.GetMethod(nameof(IUserApi.GetUserAsync))!;
        var httpGet = method.GetCustomAttribute<HttpGetAttribute>();
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(httpGet, Is.Not.Null);
        Assert.That(httpGet!.Template, Is.EqualTo("/users/{id}"));
        Assert.That(authorize, Is.Not.Null);
        Assert.That(authorize!.Roles, Is.EqualTo("Admin"));

        var parameters = method.GetParameters();
        Assert.That(parameters[0].GetCustomAttribute<FromRouteAttribute>()!.Name, Is.EqualTo("id"));
        Assert.That(parameters[1].GetCustomAttribute<FromQueryAttribute>()!.Name, Is.EqualTo("includeDeleted"));
        Assert.That(parameters[2].GetCustomAttribute<FromHeaderAttribute>()!.Name, Is.EqualTo("x-tenant"));

        var handler = new UserApiHandler();
        var controller = Activator.CreateInstance(controllerType, handler);
        var result = (Task<UserDto>)method.Invoke(controller, new object[] { 42, true, "tenant-a" })!;

        Assert.That(result.Result.Id, Is.EqualTo(42));
        Assert.That(handler.LastIncludeDeleted, Is.True);
        Assert.That(handler.LastTenant, Is.EqualTo("tenant-a"));
    }

    [Test]
    public void BuildAssembly_MapsBodyAndImplicitBindingRules()
    {
        var assembly = RuntimeControllerAssemblyBuilder.BuildAssembly(new[] { typeof(ISearchApi) });
        var controllerType = assembly.GetTypes().Single(type => type.Name == "SearchApiController");

        var getMethod = controllerType.GetMethod(nameof(ISearchApi.GetUser))!;
        var getParameters = getMethod.GetParameters();

        Assert.That(getParameters[0].GetCustomAttribute<FromRouteAttribute>()!.Name, Is.EqualTo("id"));
        Assert.That(getParameters[1].GetCustomAttribute<FromQueryAttribute>()!.Name, Is.EqualTo("culture"));

        var searchMethod = controllerType.GetMethod(nameof(ISearchApi.Search))!;
        var searchParameter = searchMethod.GetParameters().Single();

        Assert.That(searchMethod.GetCustomAttribute<HttpPostAttribute>()!.Template, Is.EqualTo("/search"));
        Assert.That(searchParameter.GetCustomAttribute<FromBodyAttribute>(), Is.Not.Null);
    }

    [Test]
    public void BuildAssembly_IgnoresUnmarkedInterfaces()
    {
        var assembly = RuntimeControllerAssemblyBuilder.BuildAssembly(new[] { typeof(IUnmarkedApi) });

        Assert.That(assembly.GetTypes(), Is.Empty);
    }

    [Test]
    public void BuildAssembly_ThrowsWhenMethodHasMultipleHttpAttributes()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RuntimeControllerAssemblyBuilder.BuildAssembly(new[] { typeof(IAmbiguousApi) }));

        Assert.That(exception!.Message, Does.Contain("multiple HTTP method attributes"));
    }

    [Test]
    public void AddRuntimeController_RegistersHandlerAndControllerApi()
    {
        var services = new ServiceCollection();

        services.AddRuntimeController<IUserApi, UserApiHandler>();

        Assert.That(services.Any(service =>
            service.ServiceType == typeof(IUserApi)
            && service.ImplementationType == typeof(UserApiHandler)
            && service.Lifetime == ServiceLifetime.Scoped), Is.True);
    }

    [Test]
    public void AddRuntimeControllers_AddsDynamicAssemblyPartForRegisteredApis()
    {
        var services = new ServiceCollection();
        services.AddRuntimeController<IUserApi, UserApiHandler>();
        services.AddControllers().AddRuntimeControllers();

        var descriptor = services.Single(service => service.ServiceType == typeof(ApplicationPartManager));
        var manager = (ApplicationPartManager)descriptor.ImplementationInstance!;

        Assert.That(manager.ApplicationParts.OfType<AssemblyPart>().Any(part =>
            part.Assembly.GetTypes().Any(type => type.Name == "UserApiController")), Is.True);
    }

    [Test]
    public void AddRestweenController_RegistersHandlerAndDynamicAssemblyPart()
    {
        var services = new ServiceCollection();

        services.AddRestweenController<IUserApi, UserApiHandler>();

        Assert.That(services.Any(service =>
            service.ServiceType == typeof(IUserApi)
            && service.ImplementationType == typeof(UserApiHandler)
            && service.Lifetime == ServiceLifetime.Scoped), Is.True);

        var descriptor = services.Single(service => service.ServiceType == typeof(ApplicationPartManager));
        var manager = (ApplicationPartManager)descriptor.ImplementationInstance!;

        Assert.That(manager.ApplicationParts.OfType<AssemblyPart>().Any(part =>
            part.Assembly.GetTypes().Any(type => type.Name == "UserApiController")), Is.True);
    }

    [Test]
    public void AddRestweenController_CanBeCalledForMultipleApisWithoutDuplicatingEndpoints()
    {
        var services = new ServiceCollection();

        services.AddRestweenController<IUserApi, UserApiHandler>();
        services.AddRestweenController<ISearchApi, SearchApiHandler>();

        var descriptor = services.Single(service => service.ServiceType == typeof(ApplicationPartManager));
        var manager = (ApplicationPartManager)descriptor.ImplementationInstance!;
        var generatedControllerNames = manager.ApplicationParts
            .OfType<AssemblyPart>()
            .SelectMany(part => part.Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.That(generatedControllerNames.Count(name => name == "UserApiController"), Is.EqualTo(1));
        Assert.That(generatedControllerNames.Count(name => name == "SearchApiController"), Is.EqualTo(1));
    }

    [Test]
    public void AddRuntimeControllers_ThrowsWhenApiHandlerIsMissing()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddControllers().AddRuntimeControllers(options => options.AddApi<IUserApi>()));

        Assert.That(exception!.Message, Does.Contain("No service is registered"));
        Assert.That(exception.Message, Does.Contain(typeof(IUserApi).FullName));
    }

    public sealed class UserDto
    {
        public int Id { get; set; }
    }

    public sealed class SearchFilter
    {
        public string? Term { get; set; }
    }

    [RestweenController]
    public interface IUserApi
    {
        [Authorize(Roles = "Admin")]
        [Get("/users/{id}")]
        Task<UserDto> GetUserAsync([ApiRoute] int id, [Query("includeDeleted")] bool includeDeleted, [Header("x-tenant")] string tenant);
    }

    public sealed class UserApiHandler : IUserApi
    {
        public bool LastIncludeDeleted { get; private set; }
        public string? LastTenant { get; private set; }

        public Task<UserDto> GetUserAsync(int id, bool includeDeleted, string tenant)
        {
            LastIncludeDeleted = includeDeleted;
            LastTenant = tenant;
            return Task.FromResult(new UserDto { Id = id });
        }
    }

    [RestweenController]
    public interface ISearchApi
    {
        [HttpGet("/users/{id}")]
        string GetUser(int id, string culture);

        [Post("/search")]
        string Search(SearchFilter filter);
    }

    public sealed class SearchApiHandler : ISearchApi
    {
        public string GetUser(int id, string culture)
        {
            return $"{id}:{culture}";
        }

        public string Search(SearchFilter filter)
        {
            return filter.Term ?? string.Empty;
        }
    }

    public interface IUnmarkedApi
    {
        [Get("/unmarked")]
        string Get();
    }

    [RestweenController]
    public interface IAmbiguousApi
    {
        [Get("/users")]
        [HttpGet("/mvc-users")]
        string GetUsers();
    }
}
