namespace ExpenseTracker.Api.Infrastructure.S3;

public sealed class LocalReceiptService(IClock clock) : IReceiptService
{
    public Task<PresignedReceiptUrl> CreateUploadUrlAsync(
        string receiptKey,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateLocalUrl(receiptKey, "PUT"));
    }

    public Task<PresignedReceiptUrl> CreateViewUrlAsync(
        string receiptKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateLocalUrl(receiptKey, "GET"));
    }

    private PresignedReceiptUrl CreateLocalUrl(string receiptKey, string method) =>
        new(
            new Uri($"https://local.receipts.test/{Uri.EscapeDataString(receiptKey)}"),
            method,
            clock.UtcNow.AddMinutes(10));
}
