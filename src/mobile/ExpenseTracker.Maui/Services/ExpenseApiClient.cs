using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Maui.Models;

namespace ExpenseTracker.Maui.Services;

public sealed class ExpenseApiClient(HttpClient http, SecureTokenStore tokenStore)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<IReadOnlyList<ExpenseReportDto>> GetMyExpensesAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Get, "/expenses");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto[]>(JsonOpts, cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<ExpenseReportDto>> GetFinanceQueueAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Get, "/finance/queue");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto[]>(JsonOpts, cancellationToken)
            ?? [];
    }

    public async Task<ExpenseReportDto> SubmitExpenseAsync(
        string expenseId,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Post, $"/expenses/{expenseId}/submit");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty submit response.");
    }

    public async Task<ExpenseReportDto> ReviewExpenseAsync(
        string expenseId,
        string decision,
        string? rejectionReason = null,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Post, $"/finance/expenses/{expenseId}/review");
        var body = rejectionReason is null
            ? $"{{\"decision\":\"{decision}\"}}"
            : $"{{\"decision\":\"{decision}\",\"rejectionReason\":\"{rejectionReason}\"}}";
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty review response.");
    }

    public async Task<ExpenseReportDto> CreateExpenseAsync(
        decimal amount,
        string currency,
        string category,
        string description,
        DateOnly expenseDate,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Post, "/expenses");
        var body = JsonSerializer.Serialize(new
        {
            amount,
            currency,
            category,
            description,
            expenseDate = expenseDate.ToString("yyyy-MM-dd")
        }, JsonOpts);
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty create response.");
    }

    public async Task<ExpenseReportDto> GetExpenseByIdAsync(
        string expenseId,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Get, $"/expenses/{expenseId}");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty expense response.");
    }

    public async Task<PresignedUrlDto> GetReceiptUploadUrlAsync(
        string expenseId,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Post, $"/expenses/{expenseId}/receipt-url");
        var body = JsonSerializer.Serialize(new
        {
            operation = "upload",
            fileName,
            contentType
        }, JsonOpts);
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PresignedUrlDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty receipt URL response.");
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{AppConfig.ApiBaseUrl}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStore.Get());
        return request;
    }
}
