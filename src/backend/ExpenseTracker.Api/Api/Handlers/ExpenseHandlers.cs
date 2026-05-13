namespace ExpenseTracker.Api.Api.Handlers;

public static class ExpenseHandlers
{
    public static ApiResponse Create(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);

    public static ApiResponse ListForEmployee(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);

    public static ApiResponse GetById(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);

    public static ApiResponse Update(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);

    public static ApiResponse Submit(RouteMatch route, string? body = null) =>
        ApiResponse.NotImplemented(route.EndpointName);
}
