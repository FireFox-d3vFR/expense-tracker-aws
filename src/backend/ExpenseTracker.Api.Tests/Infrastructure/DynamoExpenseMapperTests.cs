using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Tests.Infrastructure;

public sealed class DynamoExpenseMapperTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 5, 12, 8, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 5, 13, 9, 45, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SubmittedAt = new(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Draft_maps_to_item_without_gsi2()
    {
        var item = DynamoExpenseMapper.ToItem(Expense(ExpenseStatus.Draft));

        Assert.Equal("EXPENSE#expense-1", item.PK);
        Assert.Equal("METADATA", item.SK);
        Assert.Equal("EMPLOYEE#employee-1", item.GSI1PK);
        Assert.Equal("UPDATED#2026-05-13T09:45:00Z#EXPENSE#expense-1", item.GSI1SK);
        Assert.Null(item.GSI2PK);
        Assert.Null(item.GSI2SK);
    }

    [Fact]
    public void Submitted_maps_to_item_with_submitted_gsi2()
    {
        var item = DynamoExpenseMapper.ToItem(Expense(ExpenseStatus.Submitted));

        Assert.Equal("STATUS#Submitted", item.GSI2PK);
        Assert.Equal("SUBMITTED#2026-05-13T10:00:00Z#EXPENSE#expense-1", item.GSI2SK);
    }

    [Fact]
    public void Resubmitted_maps_to_item_with_resubmitted_gsi2()
    {
        var item = DynamoExpenseMapper.ToItem(Expense(ExpenseStatus.Resubmitted));

        Assert.Equal("STATUS#Resubmitted", item.GSI2PK);
        Assert.Equal("SUBMITTED#2026-05-13T10:00:00Z#EXPENSE#expense-1", item.GSI2SK);
    }

    [Theory]
    [InlineData(ExpenseStatus.Approved)]
    [InlineData(ExpenseStatus.Rejected)]
    public void Approved_and_rejected_map_to_item_without_gsi2(ExpenseStatus status)
    {
        var item = DynamoExpenseMapper.ToItem(Expense(status));

        Assert.Null(item.GSI2PK);
        Assert.Null(item.GSI2SK);
    }

    [Fact]
    public void Round_trip_preserves_main_domain_data()
    {
        var expense = Expense(ExpenseStatus.Submitted) with
        {
            ReceiptKey = "receipts/employee-1/expense-1/receipt.pdf",
            ReviewedAt = new DateTimeOffset(2026, 5, 14, 11, 15, 0, TimeSpan.Zero),
            ReviewedBy = "finance-1",
            RejectionReason = "Missing detail"
        };

        var mapped = DynamoExpenseMapper.ToDomain(DynamoExpenseMapper.ToItem(expense));

        Assert.Equal(expense.ExpenseId, mapped.ExpenseId);
        Assert.Equal(expense.EmployeeId, mapped.EmployeeId);
        Assert.Equal(expense.EmployeeEmail, mapped.EmployeeEmail);
        Assert.Equal(expense.Amount, mapped.Amount);
        Assert.Equal(expense.Currency, mapped.Currency);
        Assert.Equal(expense.Category, mapped.Category);
        Assert.Equal(expense.Description, mapped.Description);
        Assert.Equal(expense.ExpenseDate, mapped.ExpenseDate);
        Assert.Equal(expense.Status, mapped.Status);
        Assert.Equal(expense.ReceiptKey, mapped.ReceiptKey);
        Assert.Equal(expense.CreatedAt, mapped.CreatedAt);
        Assert.Equal(expense.UpdatedAt, mapped.UpdatedAt);
        Assert.Equal(expense.SubmittedAt, mapped.SubmittedAt);
        Assert.Equal(expense.ReviewedAt, mapped.ReviewedAt);
        Assert.Equal(expense.ReviewedBy, mapped.ReviewedBy);
        Assert.Equal(expense.RejectionReason, mapped.RejectionReason);
    }

    private static ExpenseReport Expense(ExpenseStatus status) =>
        new(
            "expense-1",
            "employee-1",
            "employee@example.test",
            status,
            42.50m,
            "EUR",
            "Meals",
            "Client lunch",
            ExpenseDate: new DateOnly(2026, 5, 11),
            CreatedAt: CreatedAt,
            UpdatedAt: UpdatedAt,
            SubmittedAt: status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted
                ? SubmittedAt
                : null);
}
