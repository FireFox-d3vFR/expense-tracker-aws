using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Infrastructure.Auth;

public static class UserContextFactory
{
    private const string SubClaim = "sub";
    private const string EmailClaim = "email";
    private const string GroupsClaim = "cognito:groups";

    public static CognitoUserContext FromClaims(IReadOnlyDictionary<string, string> claims)
    {
        if (!claims.TryGetValue(SubClaim, out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Authenticated user claims must contain a non-empty sub claim.");
        }

        claims.TryGetValue(EmailClaim, out var email);
        claims.TryGetValue(GroupsClaim, out var groups);

        return new CognitoUserContext(
            userId,
            string.IsNullOrWhiteSpace(email) ? null : email,
            ParseRoles(groups));
    }

    private static IReadOnlySet<ExpenseActorRole> ParseRoles(string? groups)
    {
        if (string.IsNullOrWhiteSpace(groups))
        {
            return new HashSet<ExpenseActorRole>();
        }

        return groups
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseRole)
            .Where(role => role is not null)
            .Cast<ExpenseActorRole>()
            .ToHashSet();
    }

    private static ExpenseActorRole? ParseRole(string group) =>
        group switch
        {
            nameof(ExpenseActorRole.Employee) => ExpenseActorRole.Employee,
            nameof(ExpenseActorRole.FinanceManager) => ExpenseActorRole.FinanceManager,
            _ => null
        };
}
