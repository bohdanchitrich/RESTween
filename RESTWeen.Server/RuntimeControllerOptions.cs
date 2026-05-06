using System;
using System.Collections.Generic;

namespace RESTween.Server;

public sealed class RuntimeControllerOptions
{
    private readonly List<Type> _apiInterfaces = new();

    internal IReadOnlyList<Type> ApiInterfaces => _apiInterfaces;

    public RuntimeControllerOptions AddApi<TApi>()
    {
        return AddApi(typeof(TApi));
    }

    public RuntimeControllerOptions AddApi(Type apiInterface)
    {
        if (apiInterface is null)
            throw new ArgumentNullException(nameof(apiInterface));

        if (!apiInterface.IsInterface)
            throw new ArgumentException("Runtime controllers can only be generated for interface types.", nameof(apiInterface));

        if (!_apiInterfaces.Contains(apiInterface))
            _apiInterfaces.Add(apiInterface);

        return this;
    }
}
