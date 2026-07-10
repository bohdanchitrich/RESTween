using System;
using System.Reflection;

namespace RESTween.Building
{
    public sealed class RestweenParameterContext
    {
        private readonly ParameterInfo[] _allParameters;
        private readonly object?[] _allValues;

        public RestweenParameterContext(
            ParameterInfo parameter,
            object? value,
            RestweenRequestMetadata metadata,
            RestweenRequestState state,
            int parameterIndex,
            ParameterInfo[]? allParameters = null,
            object?[]? allValues = null)
        {
            Parameter = parameter;
            Value = value;
            Metadata = metadata;
            State = state;
            ParameterIndex = parameterIndex;
            _allParameters = allParameters ?? new[] { parameter };
            _allValues = allValues ?? new[] { value };
        }

        public ParameterInfo Parameter { get; }

        public object? Value { get; }

        public RestweenRequestMetadata Metadata { get; }

        public RestweenRequestState State { get; }

        public int ParameterIndex { get; }

        /// <summary>
        /// Looks up the value of another parameter in the same method call by name
        /// (case-insensitive) — lets a binder correlate sibling parameters, e.g. a
        /// Stream parameter paired with a "fileName" string parameter for multipart uploads.
        /// </summary>
        public bool TryGetSiblingValue(string parameterName, out object? value)
        {
            for (var i = 0; i < _allParameters.Length; i++)
            {
                if (!string.Equals(_allParameters[i].Name, parameterName, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = _allValues[i];
                return true;
            }

            value = null;
            return false;
        }
    }
}
