using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Tests.Domain;

public sealed class ExpensePolicyTests
{
    private static readonly ExpenseActor Owner =
        new("employee-1", ExpenseActorRole.Employee);

    private static readonly ExpenseActor OtherEmployee =
        new("employee-2", ExpenseActorRole.Employee);

    private static readonly ExpenseActor FinanceManager =
        new("finance-1", ExpenseActorRole.FinanceManager);

    [Fact]
    public void Employee_owner_can_view_own_expense()
    {
        var expense = Expense(ExpenseStatus.Draft);

        Assert.True(ExpensePolicy.CanView(expense, Owner));
    }

    [Fact]
    public void Employee_non_owner_cannot_view_expense()
    {
        var expense = Expense(ExpenseStatus.Draft);

        Assert.False(ExpensePolicy.CanView(expense, OtherEmployee));
    }

    [Theory]
    [InlineData(ExpenseStatus.Draft, true)]
    [InlineData(ExpenseStatus.Rejected, true)]
    [InlineData(ExpenseStatus.Submitted, false)]
    [InlineData(ExpenseStatus.Resubmitted, false)]
    [InlineData(ExpenseStatus.Approved, false)]
    public void Employee_owner_can_edit_only_draft_or_rejected_expenses(
        ExpenseStatus status,
        bool expected)
    {
        var expense = Expense(status);

        Assert.Equal(expected, ExpensePolicy.CanEdit(expense, Owner));
    }

    [Fact]
    public void Employee_non_owner_cannot_submit_expense()
    {
        var expense = Expense(ExpenseStatus.Draft);

        Assert.False(ExpensePolicy.CanSubmit(expense, OtherEmployee));
    }

    [Fact]
    public void Employee_cannot_review_expense()
    {
        var expense = Expense(ExpenseStatus.Submitted);

        Assert.False(ExpensePolicy.CanReview(expense, Owner));
    }

    [Theory]
    [InlineData(ExpenseStatus.Submitted, true)]
    [InlineData(ExpenseStatus.Resubmitted, true)]
    [InlineData(ExpenseStatus.Draft, false)]
    [InlineData(ExpenseStatus.Rejected, false)]
    [InlineData(ExpenseStatus.Approved, false)]
    public void Finance_manager_can_review_only_submitted_or_resubmitted_expenses(
        ExpenseStatus status,
        bool expected)
    {
        var expense = Expense(status);

        Assert.Equal(expected, ExpensePolicy.CanReview(expense, FinanceManager));
    }

    private static ExpenseReport Expense(ExpenseStatus status) =>
        new("expense-1", Owner.UserId, "employee@example.test", status);
}
