namespace ExpenseTracker.Api.Contracts;

public sealed record UpdateExpenseRequest(
    decimal Amount,
    string Currency,
    string Category,
    string Description,
    DateOnly ExpenseDate);
