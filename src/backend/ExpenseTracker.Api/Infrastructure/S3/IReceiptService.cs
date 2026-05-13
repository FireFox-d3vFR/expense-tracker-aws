namespace ExpenseTracker.Api.Infrastructure.S3;

public interface IReceiptService
{
    Task<PresignedReceiptUrl> CreateUploadUrlAsync(
        string employeeId,
        string expenseId,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<PresignedReceiptUrl> CreateViewUrlAsync(
        string receiptKey,
        CancellationToken cancellationToken = default);
}

public sealed record PresignedReceiptUrl(
    Uri Url,
    string Method,
    DateTimeOffset ExpiresAt);
