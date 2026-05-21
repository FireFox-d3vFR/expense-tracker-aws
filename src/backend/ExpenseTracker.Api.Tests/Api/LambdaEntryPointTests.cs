using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using ExpenseTracker.Api.Api;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class LambdaEntryPointTests
{
    private static readonly JsonSerializerOptions JsonOptions = ExpenseTracker.Api.Api.ApiJsonOptions.Default;

    [Fact]
    public async Task Get_me_returns_api_gateway_response()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request("GET", "/me"));
        var me = JsonSerializer.Deserialize<MeResponse>(response.Body, JsonOptions);

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(me);
        Assert.Equal("employee-from-claims", me.UserId);
        Assert.Equal("employee.claims@example.test", me.Email);
        Assert.Contains(ExpenseActorRole.Employee, me.Roles);
    }

    [Fact]
    public async Task Unknown_route_returns_404_api_gateway_response()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request("GET", "/unknown"));

        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Route not found.", response.Body);
    }

    [Fact]
    public async Task Post_expenses_with_json_body_returns_created_api_gateway_response()
    {
        var entryPoint = new LambdaEntryPoint();
        var body = JsonSerializer.Serialize(new CreateExpenseRequest(
            42.50m,
            "EUR",
            "Meals",
            "Client lunch",
            new DateOnly(2026, 5, 13)), JsonOptions);

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request("POST", "/expenses", body));
        var expense = JsonSerializer.Deserialize<ExpenseResponse>(response.Body, JsonOptions);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(expense);
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Equal("employee-from-claims", expense.EmployeeId);
        Assert.Equal("employee.claims@example.test", expense.EmployeeEmail);
    }

    [Fact]
    public async Task Api_gateway_response_contains_json_content_type()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request("GET", "/unknown"));

        Assert.True(response.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task Request_without_claims_returns_clean_error()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/me"
        });

        Assert.Equal(401, response.StatusCode);
        Assert.Contains("authorizer claims", response.Body);
    }

    [Fact]
    public async Task Request_without_valid_role_returns_clean_error()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request(
            "GET",
            "/me",
            claims: new Dictionary<string, string>
            {
                ["sub"] = "user-without-role",
                ["email"] = "norole@example.test"
            }));

        Assert.Equal(401, response.StatusCode);
        Assert.Contains("explicit Employee or FinanceManager role", response.Body);
    }

    [Fact]
    public async Task Finance_manager_claims_can_access_finance_queue()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(Request(
            "GET",
            "/finance/queue",
            claims: FinanceClaims()));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("[]", response.Body);
    }

    private static APIGatewayProxyRequest Request(
        string method,
        string path,
        string? body = null,
        Dictionary<string, string>? claims = null) =>
        new()
        {
            HttpMethod = method,
            Path = path,
            Body = body,
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = claims ?? EmployeeClaims()
                }
            }
        };

    private static Dictionary<string, string> EmployeeClaims() =>
        new()
        {
            ["sub"] = "employee-from-claims",
            ["email"] = "employee.claims@example.test",
            ["cognito:groups"] = "Employee"
        };

    private static Dictionary<string, string> FinanceClaims() =>
        new()
        {
            ["sub"] = "finance-from-claims",
            ["email"] = "finance.claims@example.test",
            ["cognito:groups"] = "FinanceManager"
        };
}
