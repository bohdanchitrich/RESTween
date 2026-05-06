using System;
using System.Collections.Generic;

namespace RESTween.Server;

public sealed class RestweenRuntimeControllerOptions
{
    private readonly List<Type> _apiInterfaces = new();

    internal IReadOnlyList<Type> ApiInterfaces => _apiInterfaces;

    public RestweenRuntimeControllerOptions AddApi<TApi>()
    {
        return AddApi(typeof(TApi));
    }

    public RestweenRuntimeControllerOptions AddApi(Type apiInterface)
    {
        if (apiInterface is null)
            throw new ArgumentNullException(nameof(apiInterface));

        if (!apiInterface.IsInterface)
            throw new ArgumentException("RESTween runtime controllers can only be generated for interface types.", nameof(apiInterface));

        if (!_apiInterfaces.Contains(apiInterface))
            _apiInterfaces.Add(apiInterface);

        return this;
    }
}
