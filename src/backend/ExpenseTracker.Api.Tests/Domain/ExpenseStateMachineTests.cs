using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Tests.Domain;

public sealed class ExpenseStateMachineTests
{
    [Theory]
    [InlineData(ExpenseStatus.Draft, ExpenseStatus.Submitted)]
    [InlineData(ExpenseStatus.Rejected, ExpenseStatus.Resubmitted)]
    [InlineData(ExpenseStatus.Submitted, ExpenseStatus.Approved)]
    [InlineData(ExpenseStatus.Submitted, ExpenseStatus.Rejected)]
    [InlineData(ExpenseStatus.Resubmitted, ExpenseStatus.Approved)]
    [InlineData(ExpenseStatus.Resubmitted, ExpenseStatus.Rejected)]
    public void CanTransition_returns_true_for_mvp_transitions(
        ExpenseStatus from,
        ExpenseStatus to)
    {
        Assert.True(ExpenseStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ExpenseStatus.Draft, ExpenseStatus.Approved)]
    [InlineData(ExpenseStatus.Draft, ExpenseStatus.Rejected)]
    [InlineData(ExpenseStatus.Submitted, ExpenseStatus.Resubmitted)]
    [InlineData(ExpenseStatus.Rejected, ExpenseStatus.Approved)]
    [InlineData(ExpenseStatus.Approved, ExpenseStatus.Rejected)]
    [InlineData(ExpenseStatus.Approved, ExpenseStatus.Submitted)]
    public void CanTransition_returns_false_for_invalid_transitions(
        ExpenseStatus from,
        ExpenseStatus to)
    {
        Assert.False(ExpenseStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ExpenseStatus.Draft, ExpenseStatus.Submitted)]
    [InlineData(ExpenseStatus.Rejected, ExpenseStatus.Resubmitted)]
    public void GetSubmitTarget_maps_draft_and_rejected_to_expected_status(
        ExpenseStatus currentStatus,
        ExpenseStatus expectedTarget)
    {
        Assert.Equal(expectedTarget, ExpenseStateMachine.GetSubmitTarget(currentStatus));
    }

    [Theory]
    [InlineData(ExpenseStatus.Submitted)]
    [InlineData(ExpenseStatus.Resubmitted)]
    public void ValidateReview_allows_review_for_submitted_or_resubmitted(
        ExpenseStatus currentStatus)
    {
        var error = ExpenseStateMachine.ValidateReview(
            currentStatus,
            ReviewDecision.Approve,
            rejectionReason: null);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateReview_requires_non_empty_rejection_reason(string? rejectionReason)
    {
        var error = ExpenseStateMachine.ValidateReview(
            ExpenseStatus.Submitted,
            ReviewDecision.Reject,
            rejectionReason);

        Assert.Equal("rejection_reason_required", error?.Code);
    }

    [Fact]
    public void Approved_is_terminal()
    {
        Assert.True(ExpenseStateMachine.IsTerminal(ExpenseStatus.Approved));
        Assert.False(ExpenseStateMachine.CanReview(ExpenseStatus.Approved));
        Assert.False(ExpenseStateMachine.CanSubmit(ExpenseStatus.Approved));
    }
}
