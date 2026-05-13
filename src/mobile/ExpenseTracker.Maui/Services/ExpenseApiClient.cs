using ExpenseTracker.Maui.Models;

namespace ExpenseTracker.Maui.Services;

public sealed class ExpenseApiClient
{
    private static readonly ExpenseReportDto[] MockReports =
    [
        new(
            "expense-001",
            42.50m,
            "EUR",
            "Meals",
            "Client lunch",
            new DateOnly(2026, 5, 3),
            ExpenseStatus.Submitted,
            true),
        new(
            "expense-002",
            18.90m,
            "EUR",
            "Transport",
            "Metro tickets",
            new DateOnly(2026, 5, 5),
            ExpenseStatus.Draft,
            false),
        new(
            "expense-003",
            126.00m,
            "EUR",
            "Hotel",
            "One night business trip",
            new DateOnly(2026, 5, 8),
            ExpenseStatus.Resubmitted,
            true),
        new(
            "expense-004",
            64.30m,
            "EUR",
            "Supplies",
            "Team workshop material",
            new DateOnly(2026, 5, 10),
            ExpenseStatus.Approved,
            true)
    ];

    public Task<IReadOnlyList<ExpenseReportDto>> GetMyExpensesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExpenseReportDto>>(MockReports);

    public Task<IReadOnlyList<ExpenseReportDto>> GetFinanceQueueAsync(CancellationToken cancellationToken = default)
    {
        ExpenseReportDto[] reviewableReports = MockReports
            .Where(report => report.Status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted)
            .ToArray();

        return Task.FromResult<IReadOnlyList<ExpenseReportDto>>(reviewableReports);
    }
}
