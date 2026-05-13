using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.DynamoDb;

public sealed class DynamoExpenseRepository : IExpenseRepository
{
    public const string DefaultTableNameEnvironmentVariable = "EXPENSE_REPORTS_TABLE";
    public const string Gsi1IndexName = "GSI1";

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoExpenseRepository()
        : this(new AmazonDynamoDBClient(), ResolveTableName())
    {
    }

    public DynamoExpenseRepository(IAmazonDynamoDB dynamoDb, string tableName)
    {
        ArgumentNullException.ThrowIfNull(dynamoDb);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        _dynamoDb = dynamoDb;
        _tableName = tableName;
    }

    public async Task<ExpenseReport?> GetByIdAsync(
        string expenseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(
            CreateGetByIdRequest(_tableName, expenseId),
            cancellationToken);

        return response.Item.Count is 0
            ? null
            : DynamoExpenseMapper.ToDomain(DynamoExpenseMapper.FromAttributeMap(response.Item));
    }

    public async Task<IReadOnlyList<ExpenseReport>> ListForEmployeeAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.QueryAsync(
            CreateListForEmployeeRequest(_tableName, employeeId),
            cancellationToken);

        return response.Items
            .Select(item => DynamoExpenseMapper.ToDomain(DynamoExpenseMapper.FromAttributeMap(item)))
            .ToArray();
    }

    public async Task<ExpenseReport> CreateAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default)
    {
        await _dynamoDb.PutItemAsync(
            CreatePutRequest(
                _tableName,
                DynamoExpenseMapper.ToItem(expense),
                "attribute_not_exists(PK)"),
            cancellationToken);

        return expense;
    }

    public async Task<ExpenseReport> UpdateDraftOrRejectedAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default)
    {
        await _dynamoDb.PutItemAsync(
            CreatePutRequest(
                _tableName,
                DynamoExpenseMapper.ToItem(expense),
                "employeeId = :employeeId AND (#status = :draft OR #status = :rejected)",
                new Dictionary<string, string>
                {
                    ["#status"] = "status"
                },
                new Dictionary<string, AttributeValue>
                {
                    [":employeeId"] = new() { S = expense.EmployeeId },
                    [":draft"] = new() { S = ExpenseStatus.Draft.ToString() },
                    [":rejected"] = new() { S = ExpenseStatus.Rejected.ToString() }
                }),
            cancellationToken);

        return expense;
    }

    public async Task<ExpenseReport> SubmitAsync(
        string expenseId,
        string employeeId,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken = default)
    {
        var current = await GetByIdAsync(expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense '{expenseId}' was not found.");

        if (!string.Equals(current.EmployeeId, employeeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only the owner can submit an expense.");
        }

        var expectedStatus = current.Status;
        var submitted = current with
        {
            Status = ExpenseStateMachine.GetSubmitTarget(current.Status),
            SubmittedAt = submittedAt,
            UpdatedAt = submittedAt
        };

        await _dynamoDb.PutItemAsync(
            CreatePutRequest(
                _tableName,
                DynamoExpenseMapper.ToItem(submitted),
                "employeeId = :employeeId AND #status = :expectedStatus",
                new Dictionary<string, string>
                {
                    ["#status"] = "status"
                },
                new Dictionary<string, AttributeValue>
                {
                    [":employeeId"] = new() { S = employeeId },
                    [":expectedStatus"] = new() { S = expectedStatus.ToString() }
                }),
            cancellationToken);

        return submitted;
    }

    public Task<IReadOnlyList<ExpenseReport>> ListFinanceQueueAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Finance queue DynamoDB query will be implemented with GSI2.");

    public Task<ExpenseReport> ReviewAsync(
        string expenseId,
        ReviewDecision decision,
        string financeManagerId,
        string? rejectionReason,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Finance review DynamoDB update is outside this story.");

    public Task<ExpenseReport> AttachReceiptAsync(
        string expenseId,
        string employeeId,
        string receiptKey,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Receipt attachment DynamoDB update is outside this story.");

    public static GetItemRequest CreateGetByIdRequest(string tableName, string expenseId) =>
        new()
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = DynamoExpenseMapper.ExpensePk(expenseId) },
                ["SK"] = new() { S = "METADATA" }
            }
        };

    public static QueryRequest CreateListForEmployeeRequest(string tableName, string employeeId) =>
        new()
        {
            TableName = tableName,
            IndexName = Gsi1IndexName,
            KeyConditionExpression = "GSI1PK = :employeePk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":employeePk"] = new() { S = $"EMPLOYEE#{employeeId}" }
            },
            ScanIndexForward = false
        };

    public static PutItemRequest CreatePutRequest(
        string tableName,
        DynamoExpenseItem item,
        string conditionExpression,
        Dictionary<string, string>? expressionAttributeNames = null,
        Dictionary<string, AttributeValue>? expressionAttributeValues = null) =>
        new()
        {
            TableName = tableName,
            Item = DynamoExpenseMapper.ToAttributeMap(item),
            ConditionExpression = conditionExpression,
            ExpressionAttributeNames = expressionAttributeNames,
            ExpressionAttributeValues = expressionAttributeValues
        };

    private static string ResolveTableName() =>
        Environment.GetEnvironmentVariable(DefaultTableNameEnvironmentVariable)
        ?? throw new InvalidOperationException(
            $"Environment variable {DefaultTableNameEnvironmentVariable} must define the DynamoDB table name.");
}
