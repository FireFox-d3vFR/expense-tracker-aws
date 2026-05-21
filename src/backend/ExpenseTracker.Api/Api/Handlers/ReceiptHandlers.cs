using System.Text.Json;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;
using ExpenseTracker.Api.Infrastructure.S3;

namespace ExpenseTracker.Api.Api.Handlers;

public sealed class ReceiptHandlers(
    IExpenseRepository expenseRepository,
    IReceiptService receiptService,
    IClock clock,
    CognitoUserContext userContext)
{
    private static readonly JsonSerializerOptions JsonOptions = ApiJsonOptions.Default;

    public async Task<ApiResponse> CreateUrlAsync(
        RouteMatch route,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadJson<ReceiptUrlRequest>(body, out var request, out var error))
        {
            return ApiResponse.BadRequest(error);
        }

        var actor = userContext.ToActor();
        var expense = await expenseRepository.GetByIdAsync(route.RouteValues["expenseId"], cancellationToken);
        if (expense is null)
        {
            return ApiResponse.NotFound("Expense not found.");
        }

        return request.Operation switch
        {
            ReceiptUrlOperation.Upload => await CreateUploadUrlAsync(expense, actor, request, cancellationToken),
            ReceiptUrlOperation.View => await CreateViewUrlAsync(expense, actor, cancellationToken),
            _ => ApiResponse.BadRequest("Unsupported receipt URL operation.")
        };
    }

    private async Task<ApiResponse> CreateUploadUrlAsync(
        ExpenseReport expense,
        ExpenseActor actor,
        ReceiptUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!ExpensePolicy.CanEdit(expense, actor))
        {
            return ApiResponse.Forbidden("Only the owner can upload receipts for draft or rejected expenses.");
        }

        var receiptKey = ReceiptKeyBuilder.Build(expense.EmployeeId, expense.ExpenseId, request.FileName);
        var presignedUrl = await receiptService.CreateUploadUrlAsync(
            receiptKey,
            request.ContentType,
            cancellationToken);

        await expenseRepository.AttachReceiptAsync(
            expense.ExpenseId,
            actor.UserId,
            receiptKey,
            clock.UtcNow,
            cancellationToken);

        return ApiResponse.Ok(ToJson(ToResponse(presignedUrl)));
    }

    private async Task<ApiResponse> CreateViewUrlAsync(
        ExpenseReport expense,
        ExpenseActor actor,
        CancellationToken cancellationToken)
    {
        if (!ExpensePolicy.CanView(expense, actor))
        {
            return ApiResponse.Forbidden();
        }

        if (string.IsNullOrWhiteSpace(expense.ReceiptKey))
        {
            return ApiResponse.NotFound("Receipt not found.");
        }

        var presignedUrl = await receiptService.CreateViewUrlAsync(expense.ReceiptKey, cancellationToken);
        return ApiResponse.Ok(ToJson(ToResponse(presignedUrl)));
    }

    private static PresignedUrlResponse ToResponse(PresignedReceiptUrl url) =>
        new(url.Url.ToString(), url.Method, url.ExpiresAt);

    private static bool TryReadJson<T>(
        string? body,
        out T request,
        out string error)
        where T : class
    {
        request = null!;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(body))
        {
            error = "Request body is required.";
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(body, JsonOptions);
            if (parsed is null)
            {
                error = "Request body is invalid.";
                return false;
            }

            request = parsed;
            return true;
        }
        catch (JsonException)
        {
            error = "Request body is invalid JSON.";
            return false;
        }
    }

    private static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
