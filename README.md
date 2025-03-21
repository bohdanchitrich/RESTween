# RESTween

**RESTween** is a library for dynamically invoking REST APIs using proxy interfaces, leveraging Castle DynamicProxy. It allows for the easy creation of REST API clients with minimal code.

## Features

- Easy creation of REST API clients using proxies.
- Support for attributes to define the type of HTTP request (`GET`, `POST`, `PUT`, `DELETE`).
- Integration with any `HttpClient` for executing requests.
- Support for request and response handling through the `IRequestHandler` interface.

## Installation

To install **RESTween**, use the NuGet Package Manager or .NET CLI:

```bash
dotnet add package RESTween
```

## Usage Example

### Define an Interface

First, define an interface for your REST API using attributes that correspond to the HTTP request type:

```csharp
public interface IMyApi
{
    [Get("/users/{id}")]
    Task<User> GetUserAsync(int id);

    [Post("/users")]
    Task CreateUserAsync([Body] User user);

    [Put("/users/{id}")]
    Task UpdateUserAsync(int id, [Body] User user);

    [Delete("/users/{id}")]
    Task DeleteUserAsync(int id);
}
```

### Register the API Client

Next, register this API client in your dependency injection container:

```csharp
services.AddApiClient<IMyApi>(new Uri("https://api.example.com"));
```

### Use the API Client

Now, you can use `IMyApi` in your services:

```csharp
public class UserService
{
    private readonly IMyApi _api;

    public UserService(IMyApi api)
    {
        _api = api;
    }

    public async Task<User> GetUser(int id)
    {
        return await _api.GetUserAsync(id);
    }
}
```

## IRequestHandler Interface

`IRequestHandler`  is an interface responsible for handling HTTP requests and responses. You can implement your own handler to customize the behavior of the HTTP client, or you can use the built-in `DefaultRequestHandler`:

```csharp
public interface IRequestHandler
{
    Task<T> HandleRequestAsync<T>(HttpRequestMessage request, HttpClient httpClient);
    Task HandleRequestAsync(HttpRequestMessage request, HttpClient httpClient);
}
```

### Example of Custom `IRequestHandler` Implementation

Here’s an example of a custom `IRequestHandler` implementation that includes authentication, loading indicators, error handling, and logging of request time:

```csharp
public class HttpHandler : IRequestHandler
{
    private readonly INavigationService navigationService;
    private readonly ILoadingService loadingService;
    private readonly IAlertService alertService;
    private readonly ILocalStorageService localStorageService;
    private Stopwatch stopwatch;

    public HttpHandler(INavigationService navigationService, ILoadingService loadingService, IAlertService alertService, ILocalStorageService localStorageService)
    {
        this.navigationService = navigationService;
        this.loadingService = loadingService;
        this.alertService = alertService;
        this.localStorageService = localStorageService;
        stopwatch = new Stopwatch();
    }

    public async Task<T> HandleRequestAsync<T>(HttpRequestMessage request, HttpClient httpClient)
    {
        stopwatch.Restart();
        try
        {
            loadingService.Show();
            var token = await localStorageService.GetAuthToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            Console.WriteLine($"Sent request {request.RequestUri} with time: {stopwatch.ElapsedMilliseconds}ms");

            if (!response.IsSuccessStatusCode)
            {
                await HandleFailedResponse(response);
                return default;
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)content;
            }
            else if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            {
                return (T)Convert.ChangeType(content, typeof(T));
            }

            return JsonSerializer.Deserialize<T>(content) ?? default;
        }
        catch (Exception e)
        {
#if DEBUG
            alertService.ShowError($"{e.Message} {request.RequestUri?.ToString()}");
#else
            alertService.ShowError("Can't connect to server");
#endif
            return default;
        }
        finally
        {
            loadingService.Hide();
        }
    }

    public async Task HandleRequestAsync(HttpRequestMessage request, HttpClient httpClient)
    {
        stopwatch.Restart();
        try
        {
            loadingService.Show();
            var token = await localStorageService.GetAuthToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.SendAsync(request);
            stopwatch.Stop();
            Console.WriteLine($"Sent request {request.RequestUri} with time: {stopwatch.ElapsedMilliseconds}ms");

            if (!response.IsSuccessStatusCode)
            {
                await HandleFailedResponse(response);
            }
        }
        catch (Exception e)
        {
#if DEBUG
            alertService.ShowError($"{e.Message} {request.RequestUri?.ToString()}");
#else
            alertService.ShowError("Can't connect to server");
#endif
        }
        finally
        {
            loadingService.Hide();
        }
    }

    private async Task HandleFailedResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await localStorageService.RemoveAuthToken();
            navigationService.NavigateToLogin();
            return;
        }
        var errorResult = JsonSerializer.Deserialize<ErrorRESM>(content);
        if (errorResult == null) return;
#if DEBUG
        alertService.ShowError(errorResult.DebugMessage);
#else
        alertService.ShowError(errorResult.ReleaseMessage);
#endif
    }
}
```

### Integrating Custom `IRequestHandler`

To make your custom `IRequestHandler` work with **RESTween**, you need to register it in your dependency injection container:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register your custom IRequestHandler
    services.AddScoped<IRequestHandler, HttpHandler>();

    // Register your API client
    services.AddApiClient<IMyApi>(new Uri("https://api.example.com"));

    // Register other services used in HttpHandler
    services.AddScoped<INavigationService, NavigationService>();
    services.AddScoped<ILoadingService, LoadingService>();
    services.AddScoped<IAlertService, AlertService>();
    services.AddScoped<ILocalStorageService, LocalStorageService>();
}
```

After this setup, **RESTween** will use your `HttpHandler` to handle all requests sent through API clients created with **RESTween**.

## Attributes

**RESTween** supports several custom attributes for defining the type of HTTP request and parameters:

- **GetAttribute**: Specifies that the method performs an HTTP `GET` request.

  ```csharp
  [Get("/users/{id}")]
  ```

- **PostAttribute**: Specifies that the method performs an HTTP `POST` request.

  ```csharp
  [Post("/users")]
  ```

- **PutAttribute**: Specifies that the method performs an HTTP `PUT` request.

  ```csharp
  [Put("/users/{id}")]
  ```

- **DeleteAttribute**: Specifies that the method performs an HTTP `DELETE` request.

  ```csharp
  [Delete("/users/{id}")]
  ```

- **QueryAttribute**: Used to mark query parameters in a method.

  ```csharp
  [Get("/search")]
  Task SearchAsync([Query("term")] string searchTerm);
  ```

- **BodyAttribute**: Indicates that the method parameter should be serialized and sent in the request body.

  ```csharp
  Task CreateUserAsync([Body] User user);
  ```

## Conclusion

**RESTween** provides a convenient and flexible way to create clients for REST APIs, minimizing the amount of necessary code and simplifying the integration process. The use of attributes and the ability to customize request handling make this package a valuable tool for developers working with REST APIs in .NET.
