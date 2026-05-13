using ExpenseTracker.Maui.Models;

namespace ExpenseTracker.Maui.Services;

public sealed class ExpenseApiClient
{
    public Task<IReadOnlyList<ExpenseReportDto>> GetMyExpensesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExpenseReportDto>>(Array.Empty<ExpenseReportDto>());
}
