using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;
using ExpenseTracker.Api.Infrastructure.S3;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ExpenseTracker.Api.Api;

public sealed class LambdaEntryPoint
{
    private const string JsonContentType = "application/json";
    private readonly IExpenseRepository _expenseRepository;
    private readonly IReceiptService _receiptService;
    private readonly IClock _clock;

    public LambdaEntryPoint()
        : this(CreateRepository(), CreateReceiptService(), new SystemClock())
    {
    }

    public LambdaEntryPoint(IExpenseRepository expenseRepository, IClock clock)
        : this(expenseRepository, new LocalReceiptService(clock), clock)
    {
    }

    public LambdaEntryPoint(IExpenseRepository expenseRepository, IReceiptService receiptService, IClock clock)
    {
        _expenseRepository = expenseRepository;
        _receiptService = receiptService;
        _clock = clock;
    }

    /// <summary>AWS Lambda entry point called by the runtime via API Gateway proxy integration.</summary>
    public Task<APIGatewayProxyResponse> FunctionHandlerAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context)
        => HandleApiGatewayProxyAsync(request);

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

        var requestRouter = new RequestRouter(_expenseRepository, _clock, userContext, _receiptService);
        var response = await requestRouter.Route(
            request.HttpMethod,
            request.Path,
            request.Body,
            cancellationToken);

        return ToProxyResponse(response);
    }

    public async Task<ApiResponse> HandleAsync(
        string method,
        string path,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var router = new RequestRouter(_expenseRepository, _clock, receiptService: _receiptService);
        return await router.Route(method, path, body, cancellationToken);
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

    private static IExpenseRepository CreateRepository() =>
        Environment.GetEnvironmentVariable(DynamoExpenseRepository.DefaultTableNameEnvironmentVariable) is not null
            ? new DynamoExpenseRepository()
            : new InMemoryExpenseRepository();

    private static IReceiptService CreateReceiptService() =>
        Environment.GetEnvironmentVariable(S3ReceiptService.DefaultBucketNameEnvironmentVariable) is not null
            ? new S3ReceiptService()
            : new LocalReceiptService(new SystemClock());
}
