namespace ExpenseTracker.Api.Contracts;

public sealed record ReceiptUrlRequest(
    ReceiptUrlOperation Operation,
    string? FileName = null,
    string? ContentType = null);

public enum ReceiptUrlOperation
{
    Upload,
    View
}
