using System.Globalization;
using Amazon.DynamoDBv2.Model;
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

    public static Dictionary<string, AttributeValue> ToAttributeMap(DynamoExpenseItem item)
    {
        var attributes = new Dictionary<string, AttributeValue>
        {
            ["PK"] = S(item.PK),
            ["SK"] = S(item.SK),
            ["expenseId"] = S(item.ExpenseId),
            ["employeeId"] = S(item.EmployeeId),
            ["amount"] = N(item.Amount),
            ["currency"] = S(item.Currency),
            ["status"] = S(item.Status)
        };

        AddString(attributes, "GSI1PK", item.GSI1PK);
        AddString(attributes, "GSI1SK", item.GSI1SK);
        AddString(attributes, "GSI2PK", item.GSI2PK);
        AddString(attributes, "GSI2SK", item.GSI2SK);
        AddString(attributes, "employeeEmail", item.EmployeeEmail);
        AddString(attributes, "category", item.Category);
        AddString(attributes, "description", item.Description);
        AddString(attributes, "expenseDate", item.ExpenseDate);
        AddString(attributes, "receiptKey", item.ReceiptKey);
        AddString(attributes, "createdAt", item.CreatedAt);
        AddString(attributes, "updatedAt", item.UpdatedAt);
        AddString(attributes, "submittedAt", item.SubmittedAt);
        AddString(attributes, "reviewedAt", item.ReviewedAt);
        AddString(attributes, "reviewedBy", item.ReviewedBy);
        AddString(attributes, "rejectionReason", item.RejectionReason);

        return attributes;
    }

    public static DynamoExpenseItem FromAttributeMap(IReadOnlyDictionary<string, AttributeValue> attributes) =>
        new()
        {
            PK = GetString(attributes, "PK"),
            SK = GetString(attributes, "SK"),
            GSI1PK = GetOptionalString(attributes, "GSI1PK"),
            GSI1SK = GetOptionalString(attributes, "GSI1SK"),
            GSI2PK = GetOptionalString(attributes, "GSI2PK"),
            GSI2SK = GetOptionalString(attributes, "GSI2SK"),
            ExpenseId = GetString(attributes, "expenseId"),
            EmployeeId = GetString(attributes, "employeeId"),
            EmployeeEmail = GetOptionalString(attributes, "employeeEmail"),
            Amount = decimal.Parse(GetNumber(attributes, "amount"), CultureInfo.InvariantCulture),
            Currency = GetString(attributes, "currency"),
            Category = GetOptionalString(attributes, "category"),
            Description = GetOptionalString(attributes, "description"),
            ExpenseDate = GetOptionalString(attributes, "expenseDate"),
            Status = GetString(attributes, "status"),
            ReceiptKey = GetOptionalString(attributes, "receiptKey"),
            CreatedAt = GetOptionalString(attributes, "createdAt"),
            UpdatedAt = GetOptionalString(attributes, "updatedAt"),
            SubmittedAt = GetOptionalString(attributes, "submittedAt"),
            ReviewedAt = GetOptionalString(attributes, "reviewedAt"),
            ReviewedBy = GetOptionalString(attributes, "reviewedBy"),
            RejectionReason = GetOptionalString(attributes, "rejectionReason")
        };

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
        string expenseId)
    {
        if (status is not (ExpenseStatus.Submitted or ExpenseStatus.Resubmitted))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(submittedAt))
        {
            throw new InvalidOperationException(
                "Submitted or resubmitted expenses must have SubmittedAt to build GSI2SK.");
        }

        return $"SUBMITTED#{submittedAt}#EXPENSE#{expenseId}";
    }

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

    private static AttributeValue S(string value) =>
        new() { S = value };

    private static AttributeValue N(decimal value) =>
        new() { N = value.ToString(CultureInfo.InvariantCulture) };

    private static void AddString(
        Dictionary<string, AttributeValue> attributes,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[name] = S(value);
        }
    }

    private static string GetString(IReadOnlyDictionary<string, AttributeValue> attributes, string name) =>
        attributes[name].S;

    private static string GetNumber(IReadOnlyDictionary<string, AttributeValue> attributes, string name) =>
        attributes[name].N;

    private static string? GetOptionalString(
        IReadOnlyDictionary<string, AttributeValue> attributes,
        string name) =>
        attributes.TryGetValue(name, out var value) ? value.S : null;
}
