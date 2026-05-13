namespace ExpenseTracker.Api.Api;

public sealed class LambdaEntryPoint
{
    private readonly RequestRouter _router;

    public LambdaEntryPoint()
        : this(new RequestRouter())
    {
    }

    public LambdaEntryPoint(RequestRouter router)
    {
        _router = router;
    }

    public Task<ApiResponse> HandleAsync(
        string method,
        string path,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_router.Route(method, path, body));
    }
}
