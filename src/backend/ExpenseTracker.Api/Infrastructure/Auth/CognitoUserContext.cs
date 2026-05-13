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
        var role = IsFinanceManager switch
        {
            true => ExpenseActorRole.FinanceManager,
            false when IsEmployee => ExpenseActorRole.Employee,
            _ => throw new InvalidOperationException(
                "Authenticated user must have an explicit Employee or FinanceManager role.")
        };

        return new ExpenseActor(UserId, role);
    }
}
