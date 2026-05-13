namespace ExpenseTracker.Api.Infrastructure.DynamoDb;

public sealed record DynamoExpenseItem
{
    public string PK { get; init; } = string.Empty;

    public string SK { get; init; } = string.Empty;

    public string? GSI1PK { get; init; }

    public string? GSI1SK { get; init; }

    public string? GSI2PK { get; init; }

    public string? GSI2SK { get; init; }

    public string ExpenseId { get; init; } = string.Empty;

    public string EmployeeId { get; init; } = string.Empty;

    public string? EmployeeEmail { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    public string? Category { get; init; }

    public string? Description { get; init; }

    public string? ExpenseDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ReceiptKey { get; init; }

    public string? CreatedAt { get; init; }

    public string? UpdatedAt { get; init; }

    public string? SubmittedAt { get; init; }

    public string? ReviewedAt { get; init; }

    public string? ReviewedBy { get; init; }

    public string? RejectionReason { get; init; }
}
