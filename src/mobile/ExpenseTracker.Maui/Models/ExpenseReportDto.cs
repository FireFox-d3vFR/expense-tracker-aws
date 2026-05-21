namespace ExpenseTracker.Maui.Models;

public sealed record ExpenseReportDto(
    string ExpenseId,
    string EmployeeId,
    string EmployeeEmail,
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
    string? RejectionReason)
{
    public string StatusLabel => Status switch
    {
        ExpenseStatus.Draft => "Draft",
        ExpenseStatus.Submitted => "Submitted",
        ExpenseStatus.Rejected => "Rejected",
        ExpenseStatus.Resubmitted => "Resubmitted",
        ExpenseStatus.Approved => "Approved",
        _ => Status.ToString()
    };

    public Microsoft.Maui.Graphics.Color StatusColor => Status switch
    {
        ExpenseStatus.Approved => Microsoft.Maui.Graphics.Color.FromArgb("#34C759"),
        ExpenseStatus.Rejected => Microsoft.Maui.Graphics.Color.FromArgb("#FF3B30"),
        ExpenseStatus.Submitted or ExpenseStatus.Resubmitted => Microsoft.Maui.Graphics.Color.FromArgb("#007AFF"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#8E8E93")
    };

    public string AmountDisplay => $"{Amount:F2} {Currency}";
}
