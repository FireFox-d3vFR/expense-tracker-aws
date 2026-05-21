namespace ExpenseTracker.Maui.Models;

public sealed record MeDto(
    string UserId,
    string Email,
    ExpenseActorRole[] Roles)
{
    public bool IsEmployee => Array.IndexOf(Roles, ExpenseActorRole.Employee) >= 0;
    public bool IsFinanceManager => Array.IndexOf(Roles, ExpenseActorRole.FinanceManager) >= 0;
}

public enum ExpenseActorRole
{
    Employee,
    FinanceManager
}
