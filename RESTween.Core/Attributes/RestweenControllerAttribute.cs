using System;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class RestweenControllerAttribute : Attribute
    {
    }
}
