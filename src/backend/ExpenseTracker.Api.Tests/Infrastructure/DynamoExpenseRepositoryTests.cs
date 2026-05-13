using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Tests.Infrastructure;

public sealed class DynamoExpenseRepositoryTests
{
    [Fact]
    public void CreateGetByIdRequest_targets_table_and_primary_key()
    {
        var request = DynamoExpenseRepository.CreateGetByIdRequest("ExpenseReports", "expense-1");

        Assert.Equal("ExpenseReports", request.TableName);
        Assert.Equal("EXPENSE#expense-1", request.Key["PK"].S);
        Assert.Equal("METADATA", request.Key["SK"].S);
    }

    [Fact]
    public void CreateListForEmployeeRequest_targets_gsi1_without_scan_forward()
    {
        var request = DynamoExpenseRepository.CreateListForEmployeeRequest("ExpenseReports", "employee-1");

        Assert.Equal("ExpenseReports", request.TableName);
        Assert.Equal(DynamoExpenseRepository.Gsi1IndexName, request.IndexName);
        Assert.Equal("GSI1PK = :employeePk", request.KeyConditionExpression);
        Assert.Equal("EMPLOYEE#employee-1", request.ExpressionAttributeValues[":employeePk"].S);
        Assert.False(request.ScanIndexForward);
    }

    [Fact]
    public void CreatePutRequest_includes_condition_expression_and_mapped_item()
    {
        var item = DynamoExpenseMapper.ToItem(Expense(ExpenseStatus.Draft));

        var request = DynamoExpenseRepository.CreatePutRequest(
            "ExpenseReports",
            item,
            "attribute_not_exists(PK)");

        Assert.Equal("ExpenseReports", request.TableName);
        Assert.Equal("attribute_not_exists(PK)", request.ConditionExpression);
        Assert.Equal("EXPENSE#expense-1", request.Item["PK"].S);
        Assert.Equal("employee-1", request.Item["employeeId"].S);
        Assert.Equal("42.50", request.Item["amount"].N);
        Assert.False(request.Item.ContainsKey("GSI2PK"));
    }

    [Fact]
    public void Attribute_map_round_trip_preserves_item_fields()
    {
        var item = DynamoExpenseMapper.ToItem(Expense(ExpenseStatus.Submitted));

        var mapped = DynamoExpenseMapper.FromAttributeMap(DynamoExpenseMapper.ToAttributeMap(item));

        Assert.Equal(item.PK, mapped.PK);
        Assert.Equal(item.SK, mapped.SK);
        Assert.Equal(item.GSI1PK, mapped.GSI1PK);
        Assert.Equal(item.GSI1SK, mapped.GSI1SK);
        Assert.Equal(item.GSI2PK, mapped.GSI2PK);
        Assert.Equal(item.GSI2SK, mapped.GSI2SK);
        Assert.Equal(item.ExpenseId, mapped.ExpenseId);
        Assert.Equal(item.EmployeeId, mapped.EmployeeId);
        Assert.Equal(item.EmployeeEmail, mapped.EmployeeEmail);
        Assert.Equal(item.Status, mapped.Status);
    }

    [Fact]
    public void CreateFinanceQueueRequest_targets_gsi2_submitted_status()
    {
        var request = DynamoExpenseRepository.CreateFinanceQueueRequest(
            "ExpenseReports",
            ExpenseStatus.Submitted);

        Assert.Equal("ExpenseReports", request.TableName);
        Assert.Equal(DynamoExpenseRepository.Gsi2IndexName, request.IndexName);
        Assert.Equal("GSI2PK = :statusPk", request.KeyConditionExpression);
        Assert.Equal("STATUS#Submitted", request.ExpressionAttributeValues[":statusPk"].S);
        Assert.True(request.ScanIndexForward);
    }

    [Fact]
    public void CreateFinanceQueueRequest_targets_gsi2_resubmitted_status()
    {
        var request = DynamoExpenseRepository.CreateFinanceQueueRequest(
            "ExpenseReports",
            ExpenseStatus.Resubmitted);

        Assert.Equal(DynamoExpenseRepository.Gsi2IndexName, request.IndexName);
        Assert.Equal("STATUS#Resubmitted", request.ExpressionAttributeValues[":statusPk"].S);
    }

    [Fact]
    public void Review_approve_builds_condition_for_submitted_or_resubmitted_status()
    {
        var reviewed = DynamoExpenseRepository.ApplyReview(
            Expense(ExpenseStatus.Submitted),
            ReviewDecision.Approve,
            "finance-1",
            null,
            new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero));

        var request = DynamoExpenseRepository.CreateReviewPutRequest(
            "ExpenseReports",
            DynamoExpenseMapper.ToItem(reviewed));

        Assert.Equal(ExpenseStatus.Approved.ToString(), request.Item["status"].S);
        Assert.Equal("#status = :submitted OR #status = :resubmitted", request.ConditionExpression);
        Assert.Equal("status", request.ExpressionAttributeNames["#status"]);
        Assert.Equal("Submitted", request.ExpressionAttributeValues[":submitted"].S);
        Assert.Equal("Resubmitted", request.ExpressionAttributeValues[":resubmitted"].S);
    }

    [Fact]
    public void Review_reject_requires_rejection_reason()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DynamoExpenseRepository.ApplyReview(
                Expense(ExpenseStatus.Submitted),
                ReviewDecision.Reject,
                "finance-1",
                null,
                new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero)));

        Assert.Contains("rejection reason", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ReviewDecision.Approve, ExpenseStatus.Approved)]
    [InlineData(ReviewDecision.Reject, ExpenseStatus.Rejected)]
    public void Reviewed_items_are_removed_from_gsi2(
        ReviewDecision decision,
        ExpenseStatus expectedStatus)
    {
        var reviewed = DynamoExpenseRepository.ApplyReview(
            Expense(ExpenseStatus.Submitted),
            decision,
            "finance-1",
            decision is ReviewDecision.Reject ? "Missing receipt details." : null,
            new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero));

        var item = DynamoExpenseMapper.ToItem(reviewed);

        Assert.Equal(expectedStatus.ToString(), item.Status);
        Assert.Null(item.GSI2PK);
        Assert.Null(item.GSI2SK);
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
            ExpenseDate: new DateOnly(2026, 5, 12),
            CreatedAt: new DateTimeOffset(2026, 5, 12, 8, 0, 0, TimeSpan.Zero),
            UpdatedAt: new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
            SubmittedAt: status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted
                ? new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero)
                : null);
}
