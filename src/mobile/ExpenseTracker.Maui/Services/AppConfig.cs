namespace ExpenseTracker.Maui.Services;

public static class AppConfig
{
    public const string ApiBaseUrl = "https://drleaehgih.execute-api.eu-west-1.amazonaws.com/dev";
    public const string CognitoRegion = "eu-west-1";
    public const string CognitoClientId = "1plv938a7o29ce9bc79p4vrmf";
    public static readonly Uri CognitoEndpoint =
        new($"https://cognito-idp.{CognitoRegion}.amazonaws.com/");
}
