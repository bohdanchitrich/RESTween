# RESTween.Server

RESTween.Server is a source-generator package that creates thin ASP.NET Core controllers from RESTween API interfaces.

It is built for applications that want one shared REST contract interface and two generated adapters:

- `RESTween` on the client side builds HTTP requests from the interface.
- `RESTween.Server` on the server side generates ASP.NET Core controllers from the same interface.

Your business logic stays in a handler class that implements the interface. The generated controller only translates ASP.NET Core HTTP calls into handler method calls.

## What This Package Provides

- A Roslyn source generator shipped as an analyzer.
- `[RestweenController]` opt-in attribute generated into the consuming project.
- ASP.NET Core controller generation for interfaces marked with `[RestweenController]`.
- Mapping from RESTween HTTP attributes to ASP.NET Core MVC attributes.
- Support for standard ASP.NET Core `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, and `[HttpDelete]` attributes on API interfaces.
- Passthrough for standard `[AllowAnonymous]` and `[Authorize]` on API methods.
- Parameter binding generation for route, query, body, and header values.
- Handler-first server design: generated controllers inject the API interface and call the registered implementation.
- A dependency on `RESTween.Core`, so shared RESTween attributes are available.

## Install

```bash
dotnet add package RESTween.Server
```

The package contains the generator under `analyzers/dotnet/cs`, so it runs at compile time in the consuming project.

The consuming ASP.NET Core project must reference ASP.NET Core MVC, usually through:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

or another setup that provides `Microsoft.AspNetCore.Mvc`.

## Define a Shared Interface

```csharp
using RESTween.Attributes;
using RESTween.Server;

[RestweenController]
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<UserDto> GetUserAsync([Route] int id);

    [Post("/users")]
    Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);
}
```

Only interfaces marked with `[RestweenController]` are used by the generator.

Methods without a RESTween HTTP attribute or ASP.NET Core HTTP attribute are ignored.

You can also use standard ASP.NET Core HTTP and authorization attributes on methods:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RESTween.Attributes;
using RESTween.Server;

[RestweenController]
public interface IAccountApi
{
    [AllowAnonymous]
    [HttpPost("/login")]
    Task<LoginResult> LoginAsync(LoginDto dto);

    [Authorize(Roles = "Admin")]
    [HttpGet("/users/{id}")]
    Task<UserDto> GetUserAsync([Route] int id);
}
```

Standard ASP.NET Core authorization attributes cannot be placed on interfaces because ASP.NET Core declares them for classes and methods. Put them on API methods, or use normal ASP.NET Core policies/filters around the generated controllers.

## Implement the Handler

Write normal application code by implementing the same interface:

```csharp
public sealed class UserApiHandler : IUserApi
{
    public Task<UserDto> GetUserAsync(int id)
    {
        // Load user from your database, service, or domain layer.
        throw new NotImplementedException();
    }

    public Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Create user using your business logic.
        throw new NotImplementedException();
    }
}
```

Register the handler in DI:

```csharp
builder.Services.AddScoped<IUserApi, UserApiHandler>();
builder.Services.AddControllers();
```

Map controllers:

```csharp
app.MapControllers();
```

## What Gets Generated

For the interface above, RESTween.Server generates a controller similar to:

```csharp
[ApiController]
public sealed class UserApiController : ControllerBase
{
    private readonly IUserApi _handler;

    public UserApiController(IUserApi handler)
    {
        _handler = handler;
    }

    [HttpGet("/users/{id}")]
    public Task<UserDto> GetUserAsync([FromRoute(Name = "id")] int id)
        => _handler.GetUserAsync(id);

    [HttpPost("/users")]
    public Task<UserDto> CreateUserAsync([FromBody] CreateUserDto dto)
        => _handler.CreateUserAsync(dto);
}
```

The generated controller is intentionally thin. It should not contain business rules, persistence logic, validation policy, or mapping logic. Put that in the handler or your application layer.

## Attribute Mapping

HTTP method attributes are converted to ASP.NET Core MVC attributes:

```text
[Get("/path")]    -> [HttpGet("/path")]
[Post("/path")]   -> [HttpPost("/path")]
[Put("/path")]    -> [HttpPut("/path")]
[Delete("/path")] -> [HttpDelete("/path")]
```

Standard ASP.NET Core HTTP method attributes are accepted too:

```text
[HttpGet("/path")]    -> [HttpGet("/path")]
[HttpPost("/path")]   -> [HttpPost("/path")]
[HttpPut("/path")]    -> [HttpPut("/path")]
[HttpDelete("/path")] -> [HttpDelete("/path")]
```

Use only one HTTP method attribute per method. If a method has both RESTween and ASP.NET Core HTTP method attributes, the generator reports `RESTWEEN001`.

Authorization attributes are copied to the generated action:

```text
[AllowAnonymous] -> [AllowAnonymous]
[Authorize]      -> [Authorize]
```

`[Authorize]` constructor policy and named values such as `Roles`, `Policy`, and `AuthenticationSchemes` are preserved.

Parameter attributes are converted to MVC binding attributes:

```text
[Route]  -> [FromRoute]
[Query]  -> [FromQuery]
[Body]   -> [FromBody]
[Header] -> [FromHeader]
```

Explicit names are preserved:

```csharp
[Get("/users/{userId}")]
Task<UserDto> GetUserAsync([Route("userId")] int id);
```

Generated parameter:

```csharp
[FromRoute(Name = "userId")] int id
```

## Implicit Binding Rules

If a parameter does not have an explicit RESTween binding attribute, the generator chooses a binding using these rules:

- If the parameter name appears in the URL template as `{name}`, it becomes `[FromRoute(Name = "name")]`.
- If the parameter is a simple type, enum, string, `Guid`, `DateTime`, or nullable simple type, it becomes `[FromQuery(Name = "name")]`.
- If the method is `[Post]` or `[Put]` and the parameter is complex, it becomes `[FromBody]`.
- Otherwise, complex values become `[FromQuery(Name = "name")]`.

This keeps simple server interfaces concise while still allowing explicit attributes for public contracts.

## Controller Names

Controller names are derived from interface names:

```text
IUserApi -> UserApiController
IOrders  -> OrdersController
```

The generated controller is emitted into the same namespace as the interface.

## Recommended Project Layout

A common layout is:

```text
MyApp.Contracts
  - references RESTween.Core
  - contains IUserApi and DTOs

MyApp.Client
  - references RESTween
  - uses AddApiClient<IUserApi>(...)

MyApp.Api
  - references RESTween.Server
  - implements UserApiHandler : IUserApi
  - registers AddScoped<IUserApi, UserApiHandler>()
```

This gives you one shared contract and avoids duplicating endpoint strings between client and server.

## Limitations

The current generator focuses on controller generation for MVP server support:

- It generates ASP.NET Core MVC controllers, not Minimal APIs.
- It only processes interfaces marked with `[RestweenController]`.
- It only generates endpoints for methods with one RESTween or ASP.NET Core HTTP method attribute.
- It expects the consuming project to provide ASP.NET Core MVC references.
- It passes through ASP.NET Core authorization attributes but does not implement authorization policies by itself.
- It does not implement business logic, validation, filters, or custom response wrapping.

Use normal ASP.NET Core features around the generated controllers for authorization, filters, middleware, OpenAPI, validation, and exception handling.

## Related Packages

- `RESTween.Core`: shared RESTween attributes and contract primitives.
- `RESTween`: runtime HTTP client proxy package for calling RESTween interfaces.

Use `RESTween.Server` when your ASP.NET Core application should expose REST endpoints from the same interfaces used by RESTween clients.
