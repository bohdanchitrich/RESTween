# RESTween.Core

RESTween.Core contains the shared contract attributes used by the RESTween client and server packages.

Install this package when you want to define REST API interfaces in a shared assembly without taking a dependency on the client runtime (`RESTween`) or the server source generator (`RESTween.Server`).

## What This Package Provides

- HTTP method attributes: `[Get]`, `[Post]`, `[Put]`, `[Delete]`.
- Parameter binding attributes: `[Route]`, `[Query]`, `[Body]`, `[Header]`.
- Query collection formatting through `CollectionFormat`.
- A small `netstandard2.0` contract assembly that can be referenced by shared DTO or contract projects.

## Install

```bash
dotnet add package RESTween.Core
```

Most users do not need to install this package directly. It is installed automatically when you install `RESTween` or `RESTween.Server`.

Use it directly when you have a separate contracts project:

```text
MyApp.Contracts -> RESTween.Core
MyApp.Client    -> RESTween
MyApp.Api       -> RESTween.Server
```

## Define Shared API Contracts

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

The same interface can then be used:

- On the client side by `RESTween` to create HTTP proxy clients.
- On the server side by `RESTween.Server` to generate ASP.NET Core controllers.
- In shared libraries to keep API contracts and DTOs in one place.

## HTTP Method Attributes

Use these attributes on interface methods:

```csharp
[Get("/users/{id}")]
[Post("/users")]
[Put("/users/{id}")]
[Delete("/users/{id}")]
```

Each method attribute stores the URL template. The client package turns it into an outgoing `HttpRequestMessage`; the server package turns it into ASP.NET Core routing attributes.

## Parameter Binding Attributes

### Route

```csharp
[Get("/users/{id}")]
Task<UserDto> GetUserAsync([Route] int id);
```

You can also provide an explicit route placeholder name:

```csharp
[Get("/users/{userId}")]
Task<UserDto> GetUserAsync([Route("userId")] int id);
```

### Query

```csharp
[Get("/users")]
Task<IReadOnlyList<UserDto>> SearchAsync([Query("term")] string searchTerm);
```

Collections can use the default format or `CollectionFormat.Multi`:

```csharp
[Get("/users")]
Task<IReadOnlyList<UserDto>> SearchAsync(
    [Query("role", CollectionFormat.Multi)] string[] roles);
```

`CollectionFormat.Multi` is used by the client package to emit repeated `name[]` query keys.

### Body

```csharp
[Post("/users")]
Task<UserDto> CreateUserAsync([Body] CreateUserDto dto);
```

The client package serializes body values as JSON. The server package maps them to ASP.NET Core `[FromBody]`.

### Header

```csharp
[Get("/profile")]
Task<ProfileDto> GetProfileAsync([Header("Authorization")] string authorization);
```

The client package writes parameter values into request headers. The server package maps them to ASP.NET Core `[FromHeader]`.

## Client-Only Attributes

Some attributes are intentionally not part of `RESTween.Core` because they describe client request-building behavior rather than shared HTTP contracts.

These attributes live in the `RESTween` client package:

- `[Headers]` for static request headers.
- `[Multipart]` for `multipart/form-data` client requests.
- `[Cache]` for metadata inspected by custom client handlers.
- `[RateLimit]` for metadata inspected by custom client handlers.

They keep the same namespace, `RESTween.Attributes`, so client code can still use the same `using` directive after installing `RESTween`.

## What This Package Does Not Do

`RESTween.Core` does not:

- Send HTTP requests.
- Register DI services.
- Generate ASP.NET Core controllers.
- Serialize request or response bodies.
- Provide client-only request-building metadata such as `[Headers]`, `[Multipart]`, `[Cache]`, or `[RateLimit]`.

For HTTP clients, install `RESTween`.

For server controller generation, install `RESTween.Server`.
