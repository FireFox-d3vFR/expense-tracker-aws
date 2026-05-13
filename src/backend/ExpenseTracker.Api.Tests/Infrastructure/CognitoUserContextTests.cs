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

    [Fact]
    public void UserContextFactory_builds_context_from_simulated_claims()
    {
        var context = UserContextFactory.FromClaims(new Dictionary<string, string>
        {
            ["sub"] = "user-1",
            ["email"] = "user@example.com",
            ["cognito:groups"] = "Employee"
        });

        Assert.Equal("user-1", context.UserId);
        Assert.Equal("user@example.com", context.Email);
        Assert.True(context.IsEmployee);
        Assert.False(context.IsFinanceManager);
    }

    [Fact]
    public void UserContextFactory_builds_finance_manager_from_group_claim()
    {
        var context = UserContextFactory.FromClaims(new Dictionary<string, string>
        {
            ["sub"] = "finance-1",
            ["cognito:groups"] = "FinanceManager"
        });

        Assert.Equal("finance-1", context.UserId);
        Assert.True(context.IsFinanceManager);
    }

    [Fact]
    public void UserContextFactory_rejects_missing_sub_claim()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            UserContextFactory.FromClaims(new Dictionary<string, string>
            {
                ["cognito:groups"] = "Employee"
            }));

        Assert.Contains("sub claim", exception.Message);
    }

    private static CognitoUserContext ContextWithRoles(params ExpenseActorRole[] roles) =>
        new("user-1", "user@example.com", roles.ToHashSet());
}
