using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure.Auth;

namespace ExpenseTracker.Api.Tests.Infrastructure;

public sealed class CognitoUserContextTests
{
    [Fact]
    public void ToActor_accepts_explicit_employee_role()
    {
        var context = ContextWithRoles(ExpenseActorRole.Employee);

        var actor = context.ToActor();

        Assert.Equal("user-1", actor.UserId);
        Assert.Equal(ExpenseActorRole.Employee, actor.Role);
    }

    [Fact]
    public void ToActor_accepts_explicit_finance_manager_role()
    {
        var context = ContextWithRoles(ExpenseActorRole.FinanceManager);

        var actor = context.ToActor();

        Assert.Equal("user-1", actor.UserId);
        Assert.Equal(ExpenseActorRole.FinanceManager, actor.Role);
    }

    [Fact]
    public void ToActor_rejects_context_without_role()
    {
        var context = ContextWithRoles();

        var exception = Assert.Throws<InvalidOperationException>(() => context.ToActor());
        Assert.Contains("explicit Employee or FinanceManager role", exception.Message);
    }

    [Fact]
    public void ToActor_rejects_context_with_unknown_role()
    {
        var context = new CognitoUserContext(
            "user-1",
            "user@example.com",
            new HashSet<ExpenseActorRole> { (ExpenseActorRole)999 });

        var exception = Assert.Throws<InvalidOperationException>(() => context.ToActor());
        Assert.Contains("explicit Employee or FinanceManager role", exception.Message);
    }

    private static CognitoUserContext ContextWithRoles(params ExpenseActorRole[] roles) =>
        new("user-1", "user@example.com", roles.ToHashSet());
}
