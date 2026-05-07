using System;

namespace RESTween.Server;

internal sealed class RuntimeControllerApi
{
    public RuntimeControllerApi(Type apiInterface)
    {
        ApiInterface = apiInterface ?? throw new ArgumentNullException(nameof(apiInterface));
    }

    public Type ApiInterface { get; }
}

internal sealed class RuntimeControllerGeneratedApi
{
    public RuntimeControllerGeneratedApi(Type apiInterface)
    {
        ApiInterface = apiInterface ?? throw new ArgumentNullException(nameof(apiInterface));
    }

    public Type ApiInterface { get; }
}
