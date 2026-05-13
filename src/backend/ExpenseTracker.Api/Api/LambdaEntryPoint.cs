using Amazon.Lambda.APIGatewayEvents;

namespace ExpenseTracker.Api.Api;

public sealed class LambdaEntryPoint
{
    private const string JsonContentType = "application/json";
    private readonly RequestRouter _router;

    public LambdaEntryPoint()
        : this(new RequestRouter())
    {
    }

    public LambdaEntryPoint(RequestRouter router)
    {
        _router = router;
    }

    public async Task<ApiResponse> HandleAsync(
        string method,
        string path,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _router.Route(method, path, body, cancellationToken);
    }

    public async Task<APIGatewayProxyResponse> HandleApiGatewayProxyAsync(
        APIGatewayProxyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await HandleAsync(
            request.HttpMethod,
            request.Path,
            request.Body,
            cancellationToken);

        return new APIGatewayProxyResponse
        {
            StatusCode = response.StatusCode,
            Body = response.Body ?? string.Empty,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = JsonContentType
            }
        };
    }
}
