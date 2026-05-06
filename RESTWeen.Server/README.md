# RESTween.Server

RESTween.Server creates thin ASP.NET Core MVC controllers at runtime from RESTween API interfaces.

It is built for applications that want one shared REST contract interface and two adapters:

- `RESTween` on the client side builds HTTP requests from the interface.
- `RESTween.Server` on the server side exposes ASP.NET Core controller endpoints from the same interface.

The generated controller is created in memory with `Reflection.Emit` during service registration. Your business logic stays in a handler class that implements the interface.

## What This Package Provides

- Runtime ASP.NET Core controller generation with `Reflection.Emit`.
- `[RestweenController]` opt-in marker from `RESTween.Core`.
- A single registration method for handler DI and endpoint generation.
- Mapping from RESTween HTTP attributes to ASP.NET Core MVC attributes.
- Support for standard ASP.NET Core `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, and `[HttpDelete]` attributes on API interface methods.
- Passthrough for standard method-level `[AllowAnonymous]` and `[Authorize]`.
- Parameter binding generation for route, query, body, and header values.
- Startup validation for missing API handler registrations.

## Install

```bash
dotnet add package RESTween.Server
```

The consuming ASP.NET Core project must provide ASP.NET Core MVC, usually through:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

RESTween.Server currently targets `net8.0`.

## Define a Shared Interface

```csharp
using RESTween.Attributes;

[RestweenController]
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<UserDto> GetUserAsync([Route] int id);

    [Post("/users")]
    Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);
}
```

Only interfaces marked with `[RestweenController]` are used for runtime controller generation.

Methods without a RESTween HTTP attribute or ASP.NET Core HTTP attribute are ignored.

You can also use standard ASP.NET Core HTTP and authorization attributes on methods:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RESTween.Attributes;

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

## Register Runtime Controllers

Recommended registration:

```csharp
using RESTween.Server;

builder.Services.AddRuntimeController<IUserApi, UserApiHandler>();

builder.Services
    .AddControllers()
    .AddRuntimeControllers();

var app = builder.Build();

app.MapControllers();
```

`AddRuntimeController<TApi, THandler>()` registers `TApi -> THandler` as a scoped service and adds `TApi` to the runtime controller registry.

`AddRuntimeControllers()` builds the dynamic controller assembly and adds it to MVC through `ApplicationPartManager`.

If you already register handlers manually, you can use the explicit API form:

```csharp
builder.Services.AddScoped<IUserApi, UserApiHandler>();

builder.Services
    .AddControllers()
    .AddRuntimeControllers(options =>
    {
        options.AddApi<IUserApi>();
    });
```

If an API interface is added but no handler is registered in DI, RESTween.Server throws during service setup with a clear error message.

## What Gets Generated

For the interface above, RESTween.Server creates a runtime controller equivalent to:

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

The type is not written to disk. It is emitted into an in-memory assembly before `builder.Build()`, then MVC discovers it like a normal controller.

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

Use only one HTTP method attribute per method. If a method has both RESTween and ASP.NET Core HTTP method attributes, runtime generation throws a clear startup exception.

Authorization attributes are copied to the generated action:

```text
[AllowAnonymous] -> [AllowAnonymous]
[Authorize]      -> [Authorize]
```

`[Authorize]` constructor policy and named values such as `Roles` and `AuthenticationSchemes` are preserved.

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

If a parameter does not have an explicit RESTween binding attribute, runtime generation chooses a binding using these rules:

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

Generated controllers are emitted into the `RESTween.RuntimeGenerated` namespace.

## Recommended Project Layout

A common layout is:

```text
MyApp.Contracts
  - references RESTween.Core
  - contains IUserApi, DTOs, and the [RestweenController] marker usage

MyApp.Client
  - references RESTween
  - uses AddApiClient<IUserApi>(...)

MyApp.Api
  - references MyApp.Contracts directly
  - references RESTween.Server
  - implements UserApiHandler : IUserApi
  - registers AddRuntimeController<IUserApi, UserApiHandler>()
```

This gives you one shared contract and avoids duplicating endpoint strings between client and server.

## Limitations

- It generates ASP.NET Core MVC controllers, not Minimal APIs.
- It only processes interfaces marked with `[RestweenController]`.
- It only generates endpoints for methods with one RESTween or ASP.NET Core HTTP method attribute.
- APIs must be known during service registration, before `builder.Build()`.
- It passes through ASP.NET Core authorization attributes but does not implement authorization policies by itself.
- It does not implement business logic, validation, filters, or custom response wrapping.
- Runtime code generation with `Reflection.Emit` is not intended for NativeAOT scenarios.

Use normal ASP.NET Core features around the generated controllers for authorization, filters, middleware, OpenAPI, validation, and exception handling.

## Related Packages

- `RESTween.Core`: shared RESTween attributes and contract primitives.
- `RESTween`: runtime HTTP client proxy package for calling RESTween interfaces.

Use `RESTween.Server` when your ASP.NET Core application should expose REST endpoints from the same interfaces used by RESTween clients.
