namespace ExpenseTracker.Maui.Services;

public sealed class AuthService
{
    public Task SignInAsync(string username, string password, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Cognito authentication is not implemented yet.");
}
