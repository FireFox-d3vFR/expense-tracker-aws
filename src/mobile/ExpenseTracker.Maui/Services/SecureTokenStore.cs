namespace ExpenseTracker.Maui.Services;

public sealed class SecureTokenStore
{
    private string? _idToken;

    public void Save(string idToken) => _idToken = idToken;

    public string? Get() => _idToken;

    public void Clear() => _idToken = null;

    public bool IsAuthenticated => _idToken is not null;
}
