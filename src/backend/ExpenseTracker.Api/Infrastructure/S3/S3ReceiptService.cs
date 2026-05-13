using Amazon.S3;
using Amazon.S3.Model;

namespace ExpenseTracker.Api.Infrastructure.S3;

public sealed class S3ReceiptService : IReceiptService
{
    public const string DefaultBucketNameEnvironmentVariable = "RECEIPTS_BUCKET";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly TimeSpan _expiration;
    private readonly IClock _clock;

    public S3ReceiptService()
        : this(new AmazonS3Client(), ResolveBucketName(), new SystemClock(), DefaultExpiration)
    {
    }

    public S3ReceiptService(
        IAmazonS3 s3,
        string bucketName,
        IClock clock,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(s3);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentNullException.ThrowIfNull(clock);

        _s3 = s3;
        _bucketName = bucketName;
        _clock = clock;
        _expiration = expiration ?? DefaultExpiration;
    }

    public Task<PresignedReceiptUrl> CreateUploadUrlAsync(
        string receiptKey,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptKey);
        cancellationToken.ThrowIfCancellationRequested();

        var expiresAt = _clock.UtcNow.Add(_expiration);
        var request = CreatePresignedRequest(
            _bucketName,
            receiptKey,
            HttpVerb.PUT,
            expiresAt,
            contentType);

        return Task.FromResult(new PresignedReceiptUrl(
            new Uri(_s3.GetPreSignedURL(request)),
            "PUT",
            expiresAt));
    }

    public Task<PresignedReceiptUrl> CreateViewUrlAsync(
        string receiptKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptKey);
        cancellationToken.ThrowIfCancellationRequested();

        var expiresAt = _clock.UtcNow.Add(_expiration);
        var request = CreatePresignedRequest(_bucketName, receiptKey, HttpVerb.GET, expiresAt);

        return Task.FromResult(new PresignedReceiptUrl(
            new Uri(_s3.GetPreSignedURL(request)),
            "GET",
            expiresAt));
    }

    public static GetPreSignedUrlRequest CreatePresignedRequest(
        string bucketName,
        string receiptKey,
        HttpVerb verb,
        DateTimeOffset expiresAt,
        string? contentType = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = receiptKey,
            Verb = verb,
            Expires = expiresAt.UtcDateTime
        };

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            request.ContentType = contentType;
        }

        return request;
    }

    private static string ResolveBucketName() =>
        Environment.GetEnvironmentVariable(DefaultBucketNameEnvironmentVariable)
        ?? throw new InvalidOperationException(
            $"Environment variable {DefaultBucketNameEnvironmentVariable} must define the S3 bucket name.");
}
