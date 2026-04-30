# RESTween

RESTween is a lightweight REST API client package for .NET. It turns a C# interface into a runtime HTTP client by using Castle DynamicProxy, `HttpClient`, and RESTween attributes.

The package is designed for teams that want strongly typed API contracts without hand-writing request URLs, query strings, headers, JSON bodies, multipart forms, and response handling for every endpoint.

## What This Package Provides

- Runtime proxy clients for API interfaces.
- Attribute-based endpoint contracts with `[Get]`, `[Post]`, `[Put]`, and `[Delete]`.
- Route, query, header, body, and multipart parameter binding.
- Method-level headers through `[Headers]`.
- JSON request body serialization with `System.Text.Json`.
- Multipart upload support for `Stream`, `byte[]`, `FileInfo`, simple values, and complex JSON parts.
- Client-only metadata attributes: `[Headers]`, `[Multipart]`, `[Cache]`, and `[RateLimit]`.
- Custom request handling through `IRequestHandler`.
- Extensible request-building pipeline through DI.
- Shared contracts from `RESTween.Core`, so the same interface can be reused by client and server packages.

## Install

```bash
dotnet add package RESTween
```

`RESTween` depends on `RESTween.Core`, so the shared attributes are installed automatically.

The client package also owns client-only attributes such as `[Headers]`, `[Multipart]`, `[Cache]`, and `[RateLimit]`. They use the same `RESTween.Attributes` namespace as the shared contract attributes.

## Basic Usage

Define an API interface:

```csharp
using RESTween.Attributes;

public interface IUserApi
{
    [Get("/users/{id}")]
    Task<UserDto> GetUserAsync([Route] int id);

    [Get("/users")]
    Task<IReadOnlyList<UserDto>> SearchUsersAsync([Query] UserSearchQuery query);

    [Post("/users")]
    Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);

    [Put("/users/{id}")]
    Task<UserDto> UpdateUserAsync([Route] int id, [Body] UpdateUserDto dto);

    [Delete("/users/{id}")]
    Task DeleteUserAsync([Route] int id);
}
```

Register the client:

```csharp
using RESTween;

services.AddApiClient<IUserApi>(new Uri("https://api.example.com"));
```

Inject and use the interface:

```csharp
public sealed class UserService
{
    private readonly IUserApi _users;

    public UserService(IUserApi users)
    {
        _users = users;
    }

    public Task<UserDto> GetUserAsync(int id)
    {
        return _users.GetUserAsync(id);
    }
}
```

RESTween creates an implementation of `IUserApi` at runtime. Calling `GetUserAsync(42)` builds and sends:

```text
GET https://api.example.com/users/42
```

## Supported Interface Methods

RESTween client proxies support asynchronous methods:

```csharp
Task DoWorkAsync();
Task<T> GetValueAsync();
```

Synchronous interface methods are not supported by the client proxy and will throw `NotSupportedException`.

## Request-Building Rules

RESTween builds requests through a pipeline. The default priority is:

1. Read method metadata from `[Get]`, `[Post]`, `[Put]`, `[Delete]`, `[Multipart]`, and `[Headers]`.
2. Apply method-level headers from `[Headers("Name: Value")]`.
3. Bind each parameter with registered binders.
4. Replace route placeholders.
5. Build the query string.
6. Create the final `HttpRequestMessage`.
7. Attach JSON body, multipart content, and headers.

Explicit parameter attributes always win:

```csharp
[Route]  -> route placeholder value
[Query]  -> query string value
[Header] -> request header value
[Body]   -> JSON request body
```

If a parameter has no RESTween binding attribute, the default pipeline uses these conventions:

- If the parameter name appears in the URL template as `{name}`, it becomes a route value.
- If it is a simple type, enum, string, `Guid`, `DateTime`, or a supported collection, it becomes a query value.
- If it is a complex object on `GET` or `DELETE`, its public properties become query values.
- If it is a complex object on `POST` or `PUT`, it becomes the JSON body.
- `GET` and `DELETE` requests cannot contain a body.
- A request can have only one body parameter.
- Null query values are skipped.
- Null route values throw a `RestweenRequestBuildException`.
- Duplicate query keys throw unless `[Query(collectionFormat: CollectionFormat.Multi)]` is used.

## Route Parameters

Route values replace placeholders in the URL template:

```csharp
[Get("/users/{id}/orders/{orderId}")]
Task<OrderDto> GetOrderAsync([Route] int id, [Route] Guid orderId);
```

The `[Route]` name can be inferred from the parameter name or set explicitly:

```csharp
[Get("/users/{userId}")]
Task<UserDto> GetUserAsync([Route("userId")] int id);
```

Route values are URL-encoded.

## Query Parameters

Scalar query parameters:

```csharp
[Get("/users")]
Task<IReadOnlyList<UserDto>> GetUsersAsync([Query("active")] bool isActive);
```

Complex query DTOs:

```csharp
public sealed class UserSearchQuery
{
    public string? Term { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

[Get("/users")]
Task<IReadOnlyList<UserDto>> SearchAsync([Query] UserSearchQuery query);
```

`[JsonPropertyName]` is respected when complex query DTO properties are expanded:

```csharp
public sealed class UserSearchQuery
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}
```

Collections are supported. With `CollectionFormat.Multi`, RESTween emits repeated `name[]` keys:

```csharp
[Get("/users")]
Task<IReadOnlyList<UserDto>> GetUsersAsync(
    [Query("role", CollectionFormat.Multi)] string[] roles);
```

Example output:

```text
/users?role[]=admin&role[]=manager
```

## Headers

Use `[Headers]` for static method-level headers:

```csharp
[Headers("X-Client: mobile", "Accept-Language: en-US")]
[Get("/profile")]
Task<ProfileDto> GetProfileAsync();
```

Use `[Header]` for dynamic parameter-level headers:

```csharp
[Get("/profile")]
Task<ProfileDto> GetProfileAsync([Header("Authorization")] string bearerToken);
```

Header values are formatted using the configured `IRestweenValueFormatter`.

## Body Requests

Use `[Body]` for JSON bodies:

```csharp
[Post("/users")]
Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);
```

For `POST` and `PUT`, complex parameters without explicit attributes are also treated as body parameters:

```csharp
[Put("/users/{id}")]
Task<UserDto> UpdateUserAsync([Route] int id, UpdateUserDto dto);
```

Only one body parameter is allowed.

## Multipart Requests

Add `[Multipart]` to build `multipart/form-data` requests:

```csharp
[Multipart]
[Post("/files")]
Task<FileResultDto> UploadAsync(
    [Header("X-Trace-Id")] string traceId,
    Stream file,
    string description,
    UploadMetadata metadata);
```

The default multipart binder supports:

- `Stream` as a file part.
- `byte[]` as a file part.
- `FileInfo` as a file part with the original file name.
- Simple values as string parts.
- Complex objects as JSON parts.

## Value Formatting

Route, query, and header values are formatted consistently:

- `bool` becomes `true` or `false`.
- Enums use `[EnumMember(Value = "...")]` when available.
- `DateTime` route/query values use invariant ISO-style formatting.
- `DateTime` header values use RFC1123 formatting.
- Numbers and other `IFormattable` values use invariant culture.

## Custom Request Handling

`IRequestHandler` controls how requests are sent and how responses are processed:

```csharp
using RESTween.Handlers;

public sealed class AuthenticatedRequestHandler : IRequestHandler
{
    private readonly ITokenProvider _tokens;

    public AuthenticatedRequestHandler(ITokenProvider tokens)
    {
        _tokens = tokens;
    }

    public async Task<T> HandleRequestAsync<T>(RequestContext context, HttpClient httpClient)
    {
        var token = await _tokens.GetTokenAsync();
        context.Request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(context.Request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>() ?? default!;
    }

    public async Task HandleRequestAsync(RequestContext context, HttpClient httpClient)
    {
        var token = await _tokens.GetTokenAsync();
        context.Request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(context.Request);
        response.EnsureSuccessStatusCode();
    }
}
```

Register the handler before the API client:

```csharp
services.AddScoped<IRequestHandler, AuthenticatedRequestHandler>();
services.AddApiClient<IUserApi>(new Uri("https://api.example.com"));
```

The request handler receives a `RequestContext`, which contains:

- `Request`: the generated `HttpRequestMessage`.
- `Attributes`: the attributes found on the API method, useful for custom behavior such as caching, rate limits, retries, or logging.

Client-only metadata attributes can be read from `RequestContext`:

```csharp
var cache = context.GetAttribute<CacheAttribute>();
var rateLimit = context.GetAttribute<RateLimitAttribute>();
```

## Extending Request Building

The client request builder is split into public services under `RESTween.Building`:

- `IRestweenRequestBuilder`: builds the final `HttpRequestMessage`.
- `IRestweenParameterBinder`: binds one method parameter into route, query, header, body, or multipart state.
- `IRestweenContentSerializer`: serializes JSON bodies and multipart JSON parts.
- `IRestweenValueFormatter`: formats route, query, and header values.

`AddApiClient<T>` automatically registers default implementations using `TryAdd`, so user registrations can replace the serializer, formatter, or request builder.

Example custom serializer:

```csharp
public sealed class MyJsonSerializer : IRestweenContentSerializer
{
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public HttpContent SerializeJsonContent(object value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value, _options),
            Encoding.UTF8,
            "application/json");
    }

    public HttpContent SerializeMultipartJsonContent(object value)
    {
        return SerializeJsonContent(value);
    }
}
```

Register it:

```csharp
services.AddSingleton<IRestweenContentSerializer, MyJsonSerializer>();
services.AddApiClient<IUserApi>(new Uri("https://api.example.com"));
```

Example custom binder:

```csharp
public sealed class TenantParameterBinder : IRestweenParameterBinder
{
    public bool TryBind(RestweenParameterContext context)
    {
        if (context.Parameter.Name != "tenantId")
        {
            return false;
        }

        if (context.Value != null)
        {
            context.State.AddHeader("X-Tenant-Id", context.Value.ToString()!);
        }

        return true;
    }
}
```

Register additional binders with `AddSingleton<IRestweenParameterBinder, TenantParameterBinder>()`.

## Factory Usage Without DI

You can create a client manually:

```csharp
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.example.com")
};

var handler = new DefaultRequestHandler();
var api = ApiClientFactory.CreateClient<IUserApi>(httpClient, handler);
```

You can also pass a custom `IRestweenRequestBuilder`:

```csharp
var api = ApiClientFactory.CreateClient<IUserApi>(
    httpClient,
    handler,
    customRequestBuilder);
```

## Related Packages

- `RESTween.Core`: shared attributes and API contract primitives.
- `RESTween.Server`: source generator that creates ASP.NET Core controllers from RESTween interfaces.

Use `RESTween` when your application needs to call HTTP APIs through strongly typed interfaces.
