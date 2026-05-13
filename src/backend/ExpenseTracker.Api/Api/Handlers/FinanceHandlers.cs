namespace ExpenseTracker.Api.Api.Handlers;

public static class FinanceHandlers
{
    public static ApiResponse Queue(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);

    public static ApiResponse Review(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);
}
