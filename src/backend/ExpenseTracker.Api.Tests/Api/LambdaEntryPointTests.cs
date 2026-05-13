using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using ExpenseTracker.Api.Api;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class LambdaEntryPointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Get_me_returns_api_gateway_response()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/me",
            Body = null
        });

        Assert.Equal(501, response.StatusCode);
        Assert.Contains(EndpointNames.Me, response.Body);
    }

    [Fact]
    public async Task Unknown_route_returns_404_api_gateway_response()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/unknown",
            Body = null
        });

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

        var response = await entryPoint.HandleApiGatewayProxyAsync(new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/expenses",
            Body = body
        });
        var expense = JsonSerializer.Deserialize<ExpenseResponse>(response.Body, JsonOptions);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(expense);
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Equal("employee-1", expense.EmployeeId);
    }

    [Fact]
    public async Task Api_gateway_response_contains_json_content_type()
    {
        var entryPoint = new LambdaEntryPoint();

        var response = await entryPoint.HandleApiGatewayProxyAsync(new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/unknown"
        });

        Assert.True(response.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", response.Headers["Content-Type"]);
    }
}
