namespace ExpenseTracker.Api.Domain;

public sealed record ExpenseReport(
    string ExpenseId,
    string EmployeeId,
    ExpenseStatus Status,
    decimal Amount = 0m,
    string Currency = "EUR",
    string? Category = null,
    string? Description = null,
    string? ReceiptKey = null,
    DateOnly? ExpenseDate = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null,
    DateTimeOffset? SubmittedAt = null,
    DateTimeOffset? ReviewedAt = null,
    string? ReviewedBy = null,
    string? RejectionReason = null);
