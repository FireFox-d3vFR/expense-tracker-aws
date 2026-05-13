using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Contracts;

public sealed record ReviewExpenseRequest(
    ReviewDecision Decision,
    string? RejectionReason = null);
