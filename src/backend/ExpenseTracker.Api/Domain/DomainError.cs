namespace ExpenseTracker.Api.Domain;

public sealed record DomainError(string Code, string Message)
{
    public static DomainError InvalidTransition(ExpenseStatus from, ExpenseStatus to) =>
        new("invalid_transition", $"Cannot transition expense from {from} to {to}.");

    public static DomainError InvalidReviewState(ExpenseStatus status) =>
        new("invalid_review_state", $"Cannot review an expense with status {status}.");

    public static DomainError RejectionReasonRequired() =>
        new("rejection_reason_required", "A rejection reason is required when rejecting an expense.");

    public static DomainError Forbidden(string message = "The actor is not allowed to perform this action.") =>
        new("forbidden", message);
}
