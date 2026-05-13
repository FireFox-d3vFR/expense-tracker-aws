using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.DynamoDb;

public interface IExpenseRepository
{
    Task<ExpenseReport?> GetByIdAsync(string expenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseReport>> ListForEmployeeAsync(
        string employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseReport>> ListFinanceQueueAsync(
        CancellationToken cancellationToken = default);

    Task<ExpenseReport> CreateAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default);

    Task<ExpenseReport> UpdateDraftOrRejectedAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default);

    Task<ExpenseReport> SubmitAsync(
        string expenseId,
        string employeeId,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken = default);

    Task<ExpenseReport> ReviewAsync(
        string expenseId,
        ReviewDecision decision,
        string financeManagerId,
        string? rejectionReason,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default);

    Task<ExpenseReport> AttachReceiptAsync(
        string expenseId,
        string employeeId,
        string receiptKey,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);
}
