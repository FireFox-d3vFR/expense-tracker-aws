using System.Text.Json;
using ExpenseTracker.Api.Api;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class EmployeeExpenseHandlersTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Create_returns_created_expense()
    {
        var router = CreateRouter();
        var body = ToJson(new CreateExpenseRequest(
            42.50m,
            "EUR",
            "Meals",
            "Client lunch",
            new DateOnly(2026, 5, 3)));

        var response = router.Route("POST", "/expenses", body);
        var expense = Read<ExpenseResponse>(response);

        Assert.Equal(201, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(expense.ExpenseId));
        Assert.Equal("employee-1", expense.EmployeeId);
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Equal(42.50m, expense.Amount);
        Assert.False(expense.HasReceipt);
    }

    [Fact]
    public async Task List_returns_only_current_employee_expenses()
    {
        var repository = new InMemoryExpenseRepository();
        var router = CreateRouter(repository);
        router.Route("POST", "/expenses", ValidCreateBody("Meals"));
        await repository.CreateAsync(new ExpenseReport(
            "expense-other",
            "employee-2",
            ExpenseStatus.Draft,
            10m,
            "EUR",
            "Transport",
            "Other employee",
            ExpenseDate: new DateOnly(2026, 5, 4)));

        var response = router.Route("GET", "/expenses");
        var expenses = Read<ExpenseResponse[]>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Single(expenses);
        Assert.Equal("employee-1", expenses[0].EmployeeId);
        Assert.Equal("Meals", expenses[0].Category);
    }

    [Fact]
    public void Get_returns_employee_expense_by_id()
    {
        var router = CreateRouter();
        var created = Read<ExpenseResponse>(
            router.Route("POST", "/expenses", ValidCreateBody("Hotel")));

        var response = router.Route("GET", $"/expenses/{created.ExpenseId}");
        var expense = Read<ExpenseResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(created.ExpenseId, expense.ExpenseId);
        Assert.Equal("Hotel", expense.Category);
    }

    [Fact]
    public void Submit_moves_draft_expense_to_submitted()
    {
        var router = CreateRouter();
        var created = Read<ExpenseResponse>(
            router.Route("POST", "/expenses", ValidCreateBody("Supplies")));

        var response = router.Route("POST", $"/expenses/{created.ExpenseId}/submit");
        var submitted = Read<ExpenseResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(ExpenseStatus.Submitted, submitted.Status);
        Assert.NotNull(submitted.SubmittedAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero), submitted.SubmittedAt);
    }

    private static RequestRouter CreateRouter(InMemoryExpenseRepository? repository = null) =>
        new(
            repository ?? new InMemoryExpenseRepository(),
            new FixedClock(new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero)),
            new CognitoUserContext(
                "employee-1",
                "employee@example.test",
                new HashSet<ExpenseActorRole> { ExpenseActorRole.Employee }));

    private static string ValidCreateBody(string category) =>
        ToJson(new CreateExpenseRequest(
            99.90m,
            "EUR",
            category,
            "Demo expense",
            new DateOnly(2026, 5, 12)));

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
