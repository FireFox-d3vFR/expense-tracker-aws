using System.Text.Json;
using ExpenseTracker.Api.Api;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class FinanceHandlersTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset Now = new(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Finance_manager_can_view_queue()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense("expense-submitted", ExpenseStatus.Submitted));
        await repository.CreateAsync(Expense("expense-resubmitted", ExpenseStatus.Resubmitted));
        await repository.CreateAsync(Expense("expense-draft", ExpenseStatus.Draft));
        var router = FinanceRouter(repository);

        var response = await router.Route("GET", "/finance/queue");
        var queue = Read<ExpenseResponse[]>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(2, queue.Length);
        Assert.Contains(queue, expense => expense.ExpenseId == "expense-submitted");
        Assert.Contains(queue, expense => expense.ExpenseId == "expense-resubmitted");
        Assert.DoesNotContain(queue, expense => expense.ExpenseId == "expense-draft");
    }

    [Fact]
    public async Task Employee_cannot_view_queue()
    {
        var router = EmployeeRouter(new InMemoryExpenseRepository());

        var response = await router.Route("GET", "/finance/queue");

        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task Finance_manager_can_approve_submitted_expense()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense("expense-1", ExpenseStatus.Submitted));
        var router = FinanceRouter(repository);

        var response = await router.Route(
            "POST",
            "/finance/expenses/expense-1/review",
            ToJson(new ReviewExpenseRequest(ReviewDecision.Approve)));
        var reviewed = Read<ExpenseResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(ExpenseStatus.Approved, reviewed.Status);
        Assert.Equal("finance-1", reviewed.ReviewedBy);
        Assert.Equal(Now, reviewed.ReviewedAt);
    }

    [Fact]
    public async Task Finance_manager_can_reject_submitted_expense_with_reason()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense("expense-1", ExpenseStatus.Submitted));
        var router = FinanceRouter(repository);

        var response = await router.Route(
            "POST",
            "/finance/expenses/expense-1/review",
            ToJson(new ReviewExpenseRequest(ReviewDecision.Reject, "Missing receipt details.")));
        var reviewed = Read<ExpenseResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(ExpenseStatus.Rejected, reviewed.Status);
        Assert.Equal("Missing receipt details.", reviewed.RejectionReason);
        Assert.Equal("finance-1", reviewed.ReviewedBy);
    }

    [Fact]
    public async Task Reject_without_reason_returns_bad_request()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense("expense-1", ExpenseStatus.Submitted));
        var router = FinanceRouter(repository);

        var response = await router.Route(
            "POST",
            "/finance/expenses/expense-1/review",
            ToJson(new ReviewExpenseRequest(ReviewDecision.Reject)));

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("rejection reason", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    private static RequestRouter FinanceRouter(InMemoryExpenseRepository repository) =>
        new(
            repository,
            new FixedClock(Now),
            new CognitoUserContext(
                "finance-1",
                "finance@example.test",
                new HashSet<ExpenseActorRole> { ExpenseActorRole.FinanceManager }));

    private static RequestRouter EmployeeRouter(InMemoryExpenseRepository repository) =>
        new(
            repository,
            new FixedClock(Now),
            new CognitoUserContext(
                "employee-1",
                "employee@example.test",
                new HashSet<ExpenseActorRole> { ExpenseActorRole.Employee }));

    private static ExpenseReport Expense(string expenseId, ExpenseStatus status) =>
        new(
            expenseId,
            "employee-1",
            "employee@example.test",
            status,
            42.50m,
            "EUR",
            "Meals",
            "Client lunch",
            ExpenseDate: new DateOnly(2026, 5, 12),
            CreatedAt: Now.AddDays(-1),
            UpdatedAt: Now.AddDays(-1),
            SubmittedAt: status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted
                ? Now.AddHours(-1)
                : null);

    private static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static T Read<T>(ApiResponse response) where T : class
    {
        Assert.False(string.IsNullOrWhiteSpace(response.Body));
        return JsonSerializer.Deserialize<T>(response.Body, JsonOptions)!;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
