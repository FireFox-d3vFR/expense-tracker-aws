using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.Auth;

public sealed record CognitoUserContext(
    string UserId,
    string? Email,
    IReadOnlySet<ExpenseActorRole> Roles)
{
    public bool IsEmployee => Roles.Contains(ExpenseActorRole.Employee);

    public bool IsFinanceManager => Roles.Contains(ExpenseActorRole.FinanceManager);

    public ExpenseActor ToActor()
    {
        var role = IsFinanceManager
            ? ExpenseActorRole.FinanceManager
            : ExpenseActorRole.Employee;

        return new ExpenseActor(UserId, role);
    }
}
