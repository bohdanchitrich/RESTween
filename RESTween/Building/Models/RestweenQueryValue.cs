using RESTween.Attributes;

namespace RESTween.Building
{
    public sealed class RestweenQueryValue
    {
        public RestweenQueryValue(string name, object value, CollectionFormat collectionFormat)
        {
            Name = name;
            Value = value;
            CollectionFormat = collectionFormat;
        }

        public string Name { get; }

        public object Value { get; }

        public CollectionFormat CollectionFormat { get; }
    }
}
