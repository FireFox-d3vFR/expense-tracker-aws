namespace ExpenseTracker.Api.Domain;

public static class ExpenseStateMachine
{
    public static bool CanSubmit(ExpenseStatus status) =>
        status is ExpenseStatus.Draft or ExpenseStatus.Rejected;

    public static bool CanReview(ExpenseStatus status) =>
        status is ExpenseStatus.Submitted or ExpenseStatus.Resubmitted;

    public static bool IsTerminal(ExpenseStatus status) =>
        status is ExpenseStatus.Approved;

    public static bool CanTransition(ExpenseStatus from, ExpenseStatus to) =>
        (from, to) switch
        {
            (ExpenseStatus.Draft, ExpenseStatus.Submitted) => true,
            (ExpenseStatus.Rejected, ExpenseStatus.Resubmitted) => true,
            (ExpenseStatus.Submitted, ExpenseStatus.Approved) => true,
            (ExpenseStatus.Submitted, ExpenseStatus.Rejected) => true,
            (ExpenseStatus.Resubmitted, ExpenseStatus.Approved) => true,
            (ExpenseStatus.Resubmitted, ExpenseStatus.Rejected) => true,
            _ => false
        };

    public static ExpenseStatus GetSubmitTarget(ExpenseStatus currentStatus) =>
        currentStatus switch
        {
            ExpenseStatus.Draft => ExpenseStatus.Submitted,
            ExpenseStatus.Rejected => ExpenseStatus.Resubmitted,
            _ => throw new InvalidOperationException(
                DomainError.InvalidTransition(currentStatus, currentStatus).Message)
        };

    public static ExpenseStatus GetReviewTarget(ReviewDecision decision) =>
        decision switch
        {
            ReviewDecision.Approve => ExpenseStatus.Approved,
            ReviewDecision.Reject => ExpenseStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "Unknown review decision.")
        };

    public static DomainError? ValidateReview(
        ExpenseStatus currentStatus,
        ReviewDecision decision,
        string? rejectionReason)
    {
        if (!CanReview(currentStatus))
        {
            return DomainError.InvalidReviewState(currentStatus);
        }

        if (decision is ReviewDecision.Reject && string.IsNullOrWhiteSpace(rejectionReason))
        {
            return DomainError.RejectionReasonRequired();
        }

        return null;
    }
}
