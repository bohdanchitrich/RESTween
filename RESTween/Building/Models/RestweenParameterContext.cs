using System.Reflection;

namespace RESTween.Building
{
    public sealed class RestweenParameterContext
    {
        public RestweenParameterContext(
            ParameterInfo parameter,
            object? value,
            RestweenRequestMetadata metadata,
            RestweenRequestState state,
            int parameterIndex)
        {
            Parameter = parameter;
            Value = value;
            Metadata = metadata;
            State = state;
            ParameterIndex = parameterIndex;
        }

        public ParameterInfo Parameter { get; }

        public object? Value { get; }

        public RestweenRequestMetadata Metadata { get; }

        public RestweenRequestState State { get; }

        public int ParameterIndex { get; }
    }
}
