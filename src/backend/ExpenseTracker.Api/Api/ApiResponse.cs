namespace ExpenseTracker.Api.Api;

public sealed record ApiResponse(int StatusCode, string? Body = null)
{
    public static ApiResponse Ok(string? body = null) => new(200, body);

    public static ApiResponse NotFound(string? body = null) =>
        new(404, body ?? "Route not found.");

    public static ApiResponse NotImplemented(string endpointName) =>
        new(501, $"{endpointName} is not implemented yet.");
}
