using Amazon.Lambda.APIGatewayEvents;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Api;

public sealed class LambdaEntryPoint
{
    private const string JsonContentType = "application/json";
    private readonly RequestRouter _router;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IClock _clock;

    public LambdaEntryPoint()
        : this(new InMemoryExpenseRepository(), new SystemClock())
    {
    }

    public LambdaEntryPoint(IExpenseRepository expenseRepository, IClock clock)
        : this(expenseRepository, clock, new RequestRouter(expenseRepository, clock))
    {
    }

    public LambdaEntryPoint(RequestRouter router)
    {
        _router = router;
        _expenseRepository = new InMemoryExpenseRepository();
        _clock = new SystemClock();
    }

    private LambdaEntryPoint(
        IExpenseRepository expenseRepository,
        IClock clock,
        RequestRouter router)
    {
        _expenseRepository = expenseRepository;
        _clock = clock;
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

        CognitoUserContext userContext;
        try
        {
            userContext = BuildUserContext(request);
        }
        catch (InvalidOperationException exception)
        {
            return ToProxyResponse(new ApiResponse(401, exception.Message));
        }

        var requestRouter = new RequestRouter(_expenseRepository, _clock, userContext);
        var response = await requestRouter.Route(
            request.HttpMethod,
            request.Path,
            request.Body,
            cancellationToken);

        return ToProxyResponse(response);
    }

    private static CognitoUserContext BuildUserContext(APIGatewayProxyRequest request)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;
        if (claims is null || claims.Count is 0)
        {
            throw new InvalidOperationException("Authenticated API Gateway request must contain authorizer claims.");
        }

        var context = UserContextFactory.FromClaims(claims);
        context.ToActor();
        return context;
    }

    private static APIGatewayProxyResponse ToProxyResponse(ApiResponse response) =>
        new()
        {
            StatusCode = response.StatusCode,
            Body = response.Body ?? string.Empty,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = JsonContentType
            }
        };

}
