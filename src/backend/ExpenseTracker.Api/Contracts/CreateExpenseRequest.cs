namespace ExpenseTracker.Api.Contracts;

public sealed record CreateExpenseRequest(
    decimal Amount,
    string Currency,
    string Category,
    string Description,
    DateOnly ExpenseDate);
