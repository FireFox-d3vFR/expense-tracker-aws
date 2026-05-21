using System.Text.Json;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Infrastructure.Auth;

namespace ExpenseTracker.Api.Api.Handlers;

public sealed class MeHandler(CognitoUserContext userContext)
{
    private static readonly JsonSerializerOptions JsonOptions = ApiJsonOptions.Default;

    public ApiResponse Get(RouteMatch route, string? body = null)
    {
        userContext.ToActor();

        var response = new MeResponse(
            userContext.UserId,
            userContext.Email,
            userContext.Roles.ToArray());

        return ApiResponse.Ok(JsonSerializer.Serialize(response, JsonOptions));
    }
}
