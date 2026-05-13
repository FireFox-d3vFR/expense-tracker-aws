namespace ExpenseTracker.Api.Api.Handlers;

public static class MeHandler
{
    public static ApiResponse Get(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);
}
