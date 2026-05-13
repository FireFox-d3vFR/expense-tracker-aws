using System.Text.Json;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Api.Handlers;

public sealed class FinanceHandlers(
    IExpenseRepository expenseRepository,
    IClock clock,
    CognitoUserContext userContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResponse> QueueAsync(
        RouteMatch route,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        var actor = userContext.ToActor();
        if (actor.Role is not ExpenseActorRole.FinanceManager)
        {
            return ApiResponse.Forbidden("Only finance managers can view the finance queue.");
        }

        var queue = await expenseRepository.ListFinanceQueueAsync(cancellationToken);
        var response = queue
            .Select(expense => ToResponse(expense))
            .ToArray();

        return ApiResponse.Ok(ToJson(response));
    }

    public async Task<ApiResponse> ReviewAsync(
        RouteMatch route,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadJson<ReviewExpenseRequest>(body, out var request, out var error))
        {
            return ApiResponse.BadRequest(error);
        }

        var actor = userContext.ToActor();
        if (actor.Role is not ExpenseActorRole.FinanceManager)
        {
            return ApiResponse.Forbidden("Only finance managers can review expenses.");
        }

        var expense = await expenseRepository.GetByIdAsync(route.RouteValues["expenseId"], cancellationToken);
        if (expense is null)
        {
            return ApiResponse.NotFound("Expense not found.");
        }

        if (!ExpensePolicy.CanReview(expense, actor))
        {
            return ApiResponse.Forbidden("Expense cannot be reviewed by this user.");
        }

        var reviewError = ExpenseStateMachine.ValidateReview(
            expense.Status,
            request.Decision,
            request.RejectionReason);
        if (reviewError is not null)
        {
            return ApiResponse.BadRequest(reviewError.Message);
        }

        var reviewed = await expenseRepository.ReviewAsync(
            expense.ExpenseId,
            request.Decision,
            actor.UserId,
            request.RejectionReason,
            clock.UtcNow,
            cancellationToken);

        return ApiResponse.Ok(ToJson(ToResponse(reviewed)));
    }

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

    private static ExpenseResponse ToResponse(ExpenseReport expense) =>
        new(
            expense.ExpenseId,
            expense.EmployeeId,
            expense.EmployeeEmail,
            expense.Amount,
            expense.Currency,
            expense.Category ?? string.Empty,
            expense.Description ?? string.Empty,
            expense.ExpenseDate ?? default,
            expense.Status,
            !string.IsNullOrWhiteSpace(expense.ReceiptKey),
            expense.CreatedAt ?? default,
            expense.UpdatedAt ?? default,
            expense.SubmittedAt,
            expense.ReviewedAt,
            expense.ReviewedBy,
            expense.RejectionReason);

    private static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
