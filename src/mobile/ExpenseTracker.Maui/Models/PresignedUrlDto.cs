namespace ExpenseTracker.Maui.Models;

public sealed record PresignedUrlDto(string Url, string Method, DateTimeOffset ExpiresAt);
