namespace ExpenseTracker.Api.Contracts;

public sealed record PresignedUrlResponse(
    string Url,
    string Method,
    DateTimeOffset ExpiresAt);
