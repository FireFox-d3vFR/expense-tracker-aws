namespace ExpenseTracker.Api.Api;

public sealed record RouteMatch(
    string Method,
    string Path,
    string EndpointName,
    IReadOnlyDictionary<string, string> RouteValues)
{
    public static RouteMatch WithoutValues(string method, string path, string endpointName) =>
        new(method, path, endpointName, new Dictionary<string, string>());
}
