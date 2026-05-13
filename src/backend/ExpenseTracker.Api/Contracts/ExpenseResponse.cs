using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseResponse(
    string ExpenseId,
    string EmployeeId,
    string? EmployeeEmail,
    decimal Amount,
    string Currency,
    string Category,
    string Description,
    DateOnly ExpenseDate,
    ExpenseStatus Status,
    bool HasReceipt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewedBy,
    string? RejectionReason);
