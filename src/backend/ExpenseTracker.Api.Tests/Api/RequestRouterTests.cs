using ExpenseTracker.Api.Api;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class RequestRouterTests
{
    private readonly RequestRouter _router = new();

    [Theory]
    [InlineData("GET", "/me", EndpointNames.Me)]
    [InlineData("POST", "/expenses", EndpointNames.CreateExpense)]
    [InlineData("GET", "/expenses", EndpointNames.ListEmployeeExpenses)]
    [InlineData("GET", "/finance/queue", EndpointNames.FinanceQueue)]
    public void Match_recognizes_routes_without_expense_id(
        string method,
        string path,
        string expectedEndpoint)
    {
        var match = _router.Match(method, path);

        Assert.NotNull(match);
        Assert.Equal(expectedEndpoint, match.EndpointName);
        Assert.Empty(match.RouteValues);
    }

    [Theory]
    [InlineData("GET", "/expenses/expense-1", EndpointNames.GetExpense)]
    [InlineData("PUT", "/expenses/expense-1", EndpointNames.UpdateExpense)]
    [InlineData("POST", "/expenses/expense-1/submit", EndpointNames.SubmitExpense)]
    [InlineData("POST", "/finance/expenses/expense-1/review", EndpointNames.ReviewExpense)]
    [InlineData("POST", "/expenses/expense-1/receipt-url", EndpointNames.ReceiptUrl)]
    public void Match_recognizes_routes_with_expense_id(
        string method,
        string path,
        string expectedEndpoint)
    {
        var match = _router.Match(method, path);

        Assert.NotNull(match);
        Assert.Equal(expectedEndpoint, match.EndpointName);
        Assert.Equal("expense-1", match.RouteValues["expenseId"]);
    }

    [Fact]
    public void Match_ignores_query_string()
    {
        var match = _router.Match("GET", "/expenses/expense-1?includeReceipt=true");

        Assert.NotNull(match);
        Assert.Equal(EndpointNames.GetExpense, match.EndpointName);
        Assert.Equal("expense-1", match.RouteValues["expenseId"]);
    }

    [Fact]
    public void Match_is_case_insensitive_for_method()
    {
        var match = _router.Match("get", "/me");

        Assert.NotNull(match);
        Assert.Equal("GET", match.Method);
        Assert.Equal(EndpointNames.Me, match.EndpointName);
    }

    [Fact]
    public async Task Route_returns_not_implemented_for_known_route_skeleton()
    {
        var response = await _router.Route("GET", "/me");

        Assert.Equal(501, response.StatusCode);
        Assert.Contains(EndpointNames.Me, response.Body);
    }

    [Theory]
    [InlineData("GET", "/unknown")]
    [InlineData("DELETE", "/expenses/expense-1")]
    [InlineData("POST", "/finance/queue")]
    public async Task Route_returns_404_for_unknown_route(string method, string path)
    {
        var response = await _router.Route(method, path);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Route not found.", response.Body);
    }
}
