using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Maui.Models;

namespace ExpenseTracker.Maui.Services;

public sealed class AuthService(HttpClient http, SecureTokenStore tokenStore)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<MeDto> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var idToken = await GetIdTokenAsync(email, password, cancellationToken);
        tokenStore.Save(idToken);

        var me = await GetMeAsync(cancellationToken);
        return me;
    }

    public void SignOut() => tokenStore.Clear();

    private async Task<string> GetIdTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            AuthFlow = "USER_PASSWORD_AUTH",
            ClientId = AppConfig.CognitoClientId,
            AuthParameters = new { USERNAME = email, PASSWORD = password }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.CognitoEndpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1")
        };
        request.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

        var response = await http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Authentication failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<CognitoAuthResult>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Cognito.");

        return json.AuthenticationResult.IdToken;
    }

    private async Task<MeDto> GetMeAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConfig.ApiBaseUrl}/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStore.Get());

        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MeDto>(JsonOpts, cancellationToken)
            ?? throw new InvalidOperationException("Empty /me response.");
    }

    private sealed record CognitoAuthResult(CognitoTokens AuthenticationResult);
    private sealed record CognitoTokens(string IdToken, string AccessToken, string RefreshToken);
}
