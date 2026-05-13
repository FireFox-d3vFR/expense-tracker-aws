using ExpenseTracker.Api.Domain;

namespace ExpenseTracker.Api.Contracts;

public sealed record MeResponse(
    string UserId,
    string? Email,
    IReadOnlyCollection<ExpenseActorRole> Roles);
