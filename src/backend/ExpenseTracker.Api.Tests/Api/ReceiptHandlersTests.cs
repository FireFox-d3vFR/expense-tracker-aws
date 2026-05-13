using System.Text.Json;
using ExpenseTracker.Api.Api;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;
using ExpenseTracker.Api.Infrastructure.S3;

namespace ExpenseTracker.Api.Tests.Api;

public sealed class ReceiptHandlersTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset Now = new(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Upload_url_is_allowed_for_owner_draft_expense()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Draft));
        var receiptService = new FakeReceiptService(Now);
        var router = EmployeeRouter(repository, receiptService, "employee-1");

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.Upload, "receipt.pdf", "application/pdf")));
        var payload = Read<PresignedUrlResponse>(response);
        var updatedExpense = await repository.GetByIdAsync("expense-1");

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("PUT", payload.Method);
        Assert.DoesNotContain("receiptKey", response.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("receipts/employee-1/expense-1/receipt.pdf", receiptService.LastUploadReceiptKey);
        Assert.Equal(receiptService.LastUploadReceiptKey, updatedExpense!.ReceiptKey);
    }

    [Fact]
    public async Task Upload_url_is_refused_for_non_owner()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Draft));
        var router = EmployeeRouter(repository, new FakeReceiptService(Now), "employee-2");

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.Upload, "receipt.pdf")));

        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task Upload_url_is_refused_for_non_editable_status()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Submitted));
        var router = EmployeeRouter(repository, new FakeReceiptService(Now), "employee-1");

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.Upload, "receipt.pdf")));

        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task View_url_is_allowed_for_owner()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Submitted) with
        {
            ReceiptKey = "receipts/employee-1/expense-1/receipt.pdf"
        });
        var router = EmployeeRouter(repository, new FakeReceiptService(Now), "employee-1");

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.View)));
        var payload = Read<PresignedUrlResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("GET", payload.Method);
    }

    [Fact]
    public async Task View_url_is_allowed_for_finance_manager()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Submitted) with
        {
            ReceiptKey = "receipts/employee-1/expense-1/receipt.pdf"
        });
        var router = FinanceRouter(repository, new FakeReceiptService(Now));

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.View)));
        var payload = Read<PresignedUrlResponse>(response);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("GET", payload.Method);
    }

    [Fact]
    public async Task View_url_is_refused_when_receipt_is_missing()
    {
        var repository = new InMemoryExpenseRepository();
        await repository.CreateAsync(Expense(ExpenseStatus.Submitted));
        var router = EmployeeRouter(repository, new FakeReceiptService(Now), "employee-1");

        var response = await router.Route(
            "POST",
            "/expenses/expense-1/receipt-url",
            ToJson(new ReceiptUrlRequest(ReceiptUrlOperation.View)));

        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Receipt not found", response.Body);
    }

    private static RequestRouter EmployeeRouter(
        InMemoryExpenseRepository repository,
        IReceiptService receiptService,
        string employeeId) =>
        new(
            repository,
            new FixedClock(Now),
            new CognitoUserContext(
                employeeId,
                $"{employeeId}@example.test",
                new HashSet<ExpenseActorRole> { ExpenseActorRole.Employee }),
            receiptService);

    private static RequestRouter FinanceRouter(
        InMemoryExpenseRepository repository,
        IReceiptService receiptService) =>
        new(
            repository,
            new FixedClock(Now),
            new CognitoUserContext(
                "finance-1",
                "finance@example.test",
                new HashSet<ExpenseActorRole> { ExpenseActorRole.FinanceManager }),
            receiptService);

    private static ExpenseReport Expense(ExpenseStatus status) =>
        new(
            "expense-1",
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

    private sealed class FakeReceiptService(DateTimeOffset now) : IReceiptService
    {
        public string? LastUploadReceiptKey { get; private set; }

        public Task<PresignedReceiptUrl> CreateUploadUrlAsync(
            string receiptKey,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            LastUploadReceiptKey = receiptKey;
            return Task.FromResult(new PresignedReceiptUrl(
                new Uri("https://receipts.example.test/upload"),
                "PUT",
                now.AddMinutes(10)));
        }

        public Task<PresignedReceiptUrl> CreateViewUrlAsync(
            string receiptKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PresignedReceiptUrl(
                new Uri("https://receipts.example.test/view"),
                "GET",
                now.AddMinutes(10)));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
