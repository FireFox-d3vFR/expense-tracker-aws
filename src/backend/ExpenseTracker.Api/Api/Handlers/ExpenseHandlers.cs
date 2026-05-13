using System.Text.Json;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Domain;
using ExpenseTracker.Api.Infrastructure;
using ExpenseTracker.Api.Infrastructure.Auth;
using ExpenseTracker.Api.Infrastructure.DynamoDb;

namespace ExpenseTracker.Api.Api.Handlers;

public sealed class ExpenseHandlers(
    IExpenseRepository expenseRepository,
    IClock clock,
    CognitoUserContext userContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    // Temporary sync bridge while LambdaEntryPoint still exposes a synchronous router.
    // Replace GetAwaiter().GetResult() when the API Gateway pipeline becomes async end-to-end.

    public ApiResponse Create(RouteMatch route, string? body = null)
    {
        if (!TryReadJson<CreateExpenseRequest>(body, out var request, out var error))
        {
            return ApiResponse.BadRequest(error);
        }

        var actor = userContext.ToActor();
        if (actor.Role is not ExpenseActorRole.Employee)
        {
            return ApiResponse.Forbidden("Only employees can create expenses.");
        }

        var now = clock.UtcNow;
        var expense = new ExpenseReport(
            Guid.NewGuid().ToString("N"),
            actor.UserId,
            ExpenseStatus.Draft,
            request.Amount,
            request.Currency,
            request.Category,
            request.Description,
            ExpenseDate: request.ExpenseDate,
            CreatedAt: now,
            UpdatedAt: now);

        var created = expenseRepository.CreateAsync(expense).GetAwaiter().GetResult();
        return ApiResponse.Created(ToJson(ToResponse(created, userContext.Email)));
    }

    public ApiResponse ListForEmployee(RouteMatch route, string? body = null)
    {
        var actor = userContext.ToActor();
        if (actor.Role is not ExpenseActorRole.Employee)
        {
            return ApiResponse.Forbidden("Only employees can list their expenses.");
        }

        var expenses = expenseRepository
            .ListForEmployeeAsync(actor.UserId)
            .GetAwaiter()
            .GetResult()
            .Select(expense => ToResponse(expense, userContext.Email))
            .ToArray();

        return ApiResponse.Ok(ToJson(expenses));
    }

    public ApiResponse GetById(RouteMatch route, string? body = null)
    {
        var actor = userContext.ToActor();
        var expense = GetExpense(route);
        if (expense is null)
        {
            return ApiResponse.NotFound("Expense not found.");
        }

        if (!ExpensePolicy.CanView(expense, actor))
        {
            return ApiResponse.Forbidden();
        }

        return ApiResponse.Ok(ToJson(ToResponse(expense, userContext.Email)));
    }

    public ApiResponse Update(RouteMatch route, string? body = null)
    {
        if (!TryReadJson<UpdateExpenseRequest>(body, out var request, out var error))
        {
            return ApiResponse.BadRequest(error);
        }

        var actor = userContext.ToActor();
        var existing = GetExpense(route);
        if (existing is null)
        {
            return ApiResponse.NotFound("Expense not found.");
        }

        if (!ExpensePolicy.CanEdit(existing, actor))
        {
            return ApiResponse.Forbidden("Expense cannot be edited by this user.");
        }

        var updated = existing with
        {
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate,
            UpdatedAt = clock.UtcNow
        };

        var saved = expenseRepository.UpdateDraftOrRejectedAsync(updated).GetAwaiter().GetResult();
        return ApiResponse.Ok(ToJson(ToResponse(saved, userContext.Email)));
    }

    public ApiResponse Submit(RouteMatch route, string? body = null)
    {
        var actor = userContext.ToActor();
        var existing = GetExpense(route);
        if (existing is null)
        {
            return ApiResponse.NotFound("Expense not found.");
        }

        if (!ExpensePolicy.CanSubmit(existing, actor))
        {
            return ApiResponse.Forbidden("Expense cannot be submitted by this user.");
        }

        var submitted = expenseRepository
            .SubmitAsync(existing.ExpenseId, actor.UserId, clock.UtcNow)
            .GetAwaiter()
            .GetResult();

        return ApiResponse.Ok(ToJson(ToResponse(submitted, userContext.Email)));
    }

    private ExpenseReport? GetExpense(RouteMatch route) =>
        expenseRepository
            .GetByIdAsync(route.RouteValues["expenseId"])
            .GetAwaiter()
            .GetResult();

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

    private static ExpenseResponse ToResponse(ExpenseReport expense, string? employeeEmail) =>
        new(
            expense.ExpenseId,
            expense.EmployeeId,
            employeeEmail,
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
