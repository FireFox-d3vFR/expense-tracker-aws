namespace ExpenseTracker.Api.Domain;

public enum ExpenseActorRole
{
    Employee,
    FinanceManager
}

public sealed record ExpenseActor(string UserId, ExpenseActorRole Role);

public static class ExpensePolicy
{
    public static bool IsOwner(ExpenseReport expense, ExpenseActor actor) =>
        string.Equals(expense.EmployeeId, actor.UserId, StringComparison.Ordinal);

    public static bool CanView(ExpenseReport expense, ExpenseActor actor) =>
        actor.Role is ExpenseActorRole.FinanceManager ||
        actor.Role is ExpenseActorRole.Employee && IsOwner(expense, actor);

    public static bool CanEdit(ExpenseReport expense, ExpenseActor actor) =>
        actor.Role is ExpenseActorRole.Employee &&
        IsOwner(expense, actor) &&
        expense.Status is ExpenseStatus.Draft or ExpenseStatus.Rejected;

    public static bool CanSubmit(ExpenseReport expense, ExpenseActor actor) =>
        actor.Role is ExpenseActorRole.Employee &&
        IsOwner(expense, actor) &&
        ExpenseStateMachine.CanSubmit(expense.Status);

    public static bool CanReview(ExpenseReport expense, ExpenseActor actor) =>
        actor.Role is ExpenseActorRole.FinanceManager &&
        ExpenseStateMachine.CanReview(expense.Status);
}
