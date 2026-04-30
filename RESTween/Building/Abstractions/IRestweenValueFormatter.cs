namespace RESTween.Building
{
    public interface IRestweenValueFormatter
    {
        string FormatRouteValue(object value);

        string FormatQueryValue(object value);

        string FormatHeaderValue(object value);
    }
}
