using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.DynamoDb;

public sealed class InMemoryExpenseRepository : IExpenseRepository
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, ExpenseReport> _expenses = new(StringComparer.Ordinal);

    public Task<ExpenseReport?> GetByIdAsync(
        string expenseId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_expenses.GetValueOrDefault(expenseId));
        }
    }

    public Task<IReadOnlyList<ExpenseReport>> ListForEmployeeAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<ExpenseReport>>(
                _expenses.Values
                    .Where(expense => string.Equals(expense.EmployeeId, employeeId, StringComparison.Ordinal))
                    .OrderByDescending(expense => expense.CreatedAt)
                    .ToArray());
        }
    }

    public Task<IReadOnlyList<ExpenseReport>> ListFinanceQueueAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<ExpenseReport>>(
                _expenses.Values
                    .Where(expense => ExpenseStateMachine.CanReview(expense.Status))
                    .OrderBy(expense => expense.SubmittedAt)
                    .ToArray());
        }
    }

    public Task<ExpenseReport> CreateAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _expenses.Add(expense.ExpenseId, expense);
            return Task.FromResult(expense);
        }
    }

    public Task<ExpenseReport> UpdateDraftOrRejectedAsync(
        ExpenseReport expense,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var current = GetRequired(expense.ExpenseId);
            if (current.Status is not (ExpenseStatus.Draft or ExpenseStatus.Rejected))
            {
                throw new InvalidOperationException("Only draft or rejected expenses can be updated.");
            }

            _expenses[expense.ExpenseId] = expense;
            return Task.FromResult(expense);
        }
    }

    public Task<ExpenseReport> SubmitAsync(
        string expenseId,
        string employeeId,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var current = GetRequired(expenseId);
            if (!string.Equals(current.EmployeeId, employeeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Only the owner can submit an expense.");
            }

            var nextStatus = ExpenseStateMachine.GetSubmitTarget(current.Status);
            var submitted = current with
            {
                Status = nextStatus,
                SubmittedAt = submittedAt,
                UpdatedAt = submittedAt
            };

            _expenses[expenseId] = submitted;
            return Task.FromResult(submitted);
        }
    }

    public Task<ExpenseReport> ReviewAsync(
        string expenseId,
        ReviewDecision decision,
        string financeManagerId,
        string? rejectionReason,
        DateTimeOffset reviewedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var current = GetRequired(expenseId);
            var reviewError = ExpenseStateMachine.ValidateReview(current.Status, decision, rejectionReason);
            if (reviewError is not null)
            {
                throw new InvalidOperationException(reviewError.Message);
            }

            var reviewed = current with
            {
                Status = ExpenseStateMachine.GetReviewTarget(decision),
                ReviewedAt = reviewedAt,
                ReviewedBy = financeManagerId,
                RejectionReason = rejectionReason,
                UpdatedAt = reviewedAt
            };

            _expenses[expenseId] = reviewed;
            return Task.FromResult(reviewed);
        }
    }

    public Task<ExpenseReport> AttachReceiptAsync(
        string expenseId,
        string employeeId,
        string receiptKey,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var current = GetRequired(expenseId);
            if (!string.Equals(current.EmployeeId, employeeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Only the owner can attach a receipt.");
            }

            var updated = current with
            {
                ReceiptKey = receiptKey,
                UpdatedAt = updatedAt
            };

            _expenses[expenseId] = updated;
            return Task.FromResult(updated);
        }
    }

    private ExpenseReport GetRequired(string expenseId) =>
        _expenses.GetValueOrDefault(expenseId)
        ?? throw new KeyNotFoundException($"Expense '{expenseId}' was not found.");
}
