using System.Globalization;
using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.DynamoDb;

public static class DynamoExpenseMapper
{
    private const string MetadataSortKey = "METADATA";

    public static DynamoExpenseItem ToItem(ExpenseReport expense)
    {
        var updatedAt = FormatDateTime(expense.UpdatedAt);
        var submittedAt = FormatDateTime(expense.SubmittedAt);

        return new DynamoExpenseItem
        {
            PK = ExpensePk(expense.ExpenseId),
            SK = MetadataSortKey,
            GSI1PK = EmployeePk(expense.EmployeeId),
            GSI1SK = $"UPDATED#{updatedAt}#EXPENSE#{expense.ExpenseId}",
            GSI2PK = FinanceQueueStatusPk(expense.Status),
            GSI2SK = FinanceQueueSortKey(expense.Status, submittedAt, expense.ExpenseId),
            ExpenseId = expense.ExpenseId,
            EmployeeId = expense.EmployeeId,
            EmployeeEmail = expense.EmployeeEmail,
            Amount = expense.Amount,
            Currency = expense.Currency,
            Category = expense.Category,
            Description = expense.Description,
            ExpenseDate = FormatDate(expense.ExpenseDate),
            Status = expense.Status.ToString(),
            ReceiptKey = expense.ReceiptKey,
            CreatedAt = FormatDateTime(expense.CreatedAt),
            UpdatedAt = updatedAt,
            SubmittedAt = submittedAt,
            ReviewedAt = FormatDateTime(expense.ReviewedAt),
            ReviewedBy = expense.ReviewedBy,
            RejectionReason = expense.RejectionReason
        };
    }

    public static ExpenseReport ToDomain(DynamoExpenseItem item) =>
        new(
            item.ExpenseId,
            item.EmployeeId,
            item.EmployeeEmail,
            ParseStatus(item.Status),
            item.Amount,
            item.Currency,
            item.Category,
            item.Description,
            item.ReceiptKey,
            ParseDate(item.ExpenseDate),
            ParseDateTime(item.CreatedAt),
            ParseDateTime(item.UpdatedAt),
            ParseDateTime(item.SubmittedAt),
            ParseDateTime(item.ReviewedAt),
            item.ReviewedBy,
            item.RejectionReason);

    public static string ExpensePk(string expenseId) =>
        $"EXPENSE#{expenseId}";

    private static string EmployeePk(string employeeId) =>
        $"EMPLOYEE#{employeeId}";

    private static string? FinanceQueueStatusPk(ExpenseStatus status) =>
        status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted
            ? $"STATUS#{status}"
            : null;

    private static string? FinanceQueueSortKey(
        ExpenseStatus status,
        string? submittedAt,
        string expenseId) =>
        status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted
            ? $"SUBMITTED#{submittedAt}#EXPENSE#{expenseId}"
            : null;

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatDateTime(DateTimeOffset? value) =>
        value?.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static DateOnly? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTimeOffset? ParseDateTime(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateTimeOffset.ParseExact(
                value,
                "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);

    private static ExpenseStatus ParseStatus(string status) =>
        Enum.Parse<ExpenseStatus>(status, ignoreCase: false);
}
