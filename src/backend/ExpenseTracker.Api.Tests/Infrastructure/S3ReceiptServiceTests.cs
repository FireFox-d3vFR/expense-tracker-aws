using Amazon.S3;
using Amazon.S3.Model;
using ExpenseTracker.Api.Infrastructure.S3;

namespace ExpenseTracker.Api.Tests.Infrastructure;

public sealed class S3ReceiptServiceTests
{
    [Fact]
    public void CreatePresignedRequest_builds_put_upload_request()
    {
        var expiresAt = new DateTimeOffset(2026, 5, 13, 10, 10, 0, TimeSpan.Zero);

        var request = S3ReceiptService.CreatePresignedRequest(
            "receipts-bucket",
            "receipts/employee-1/expense-1/receipt.pdf",
            HttpVerb.PUT,
            expiresAt,
            "application/pdf");

        Assert.Equal("receipts-bucket", request.BucketName);
        Assert.Equal("receipts/employee-1/expense-1/receipt.pdf", request.Key);
        Assert.Equal(HttpVerb.PUT, request.Verb);
        Assert.Equal("application/pdf", request.ContentType);
        Assert.Equal(expiresAt.UtcDateTime, request.Expires);
    }

    [Fact]
    public void CreatePresignedRequest_builds_get_view_request()
    {
        var expiresAt = new DateTimeOffset(2026, 5, 13, 10, 10, 0, TimeSpan.Zero);

        var request = S3ReceiptService.CreatePresignedRequest(
            "receipts-bucket",
            "receipts/employee-1/expense-1/receipt.pdf",
            HttpVerb.GET,
            expiresAt);

        Assert.Equal(HttpVerb.GET, request.Verb);
        Assert.Null(request.ContentType);
    }
}
