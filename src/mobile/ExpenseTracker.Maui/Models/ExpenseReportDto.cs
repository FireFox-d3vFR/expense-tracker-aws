namespace ExpenseTracker.Maui.Models;

public sealed record ExpenseReportDto(
    string ExpenseId,
    decimal Amount,
    string Currency,
    string Category,
    string Description,
    DateOnly ExpenseDate,
    ExpenseStatus Status,
    bool HasReceipt);
