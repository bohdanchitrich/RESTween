# RESTween

RESTween is a library for dynamically invoking REST APIs using proxy interfaces, leveraging Castle DynamicProxy. It allows for the easy creation of REST API clients with minimal code.

## Features

- Easy creation of REST API clients using proxies.
- Support for attributes to define the type of HTTP request (GET, POST).
- Integration with any `HttpClient` for executing requests.
- Support for request and response handling through the `IRequestHandler` interface.

## Installation

To install RESTween, use the NuGet Package Manager or .NET CLI:

```bash
dotnet add package RESTween
