namespace RESTween.Building
{
    public interface IRestweenParameterBinder
    {
        bool TryBind(RestweenParameterContext context);
    }
}
