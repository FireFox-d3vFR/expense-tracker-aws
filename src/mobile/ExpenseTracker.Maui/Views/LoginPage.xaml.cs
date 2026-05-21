using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Email and password are required.");
            return;
        }

        SetLoading(true);
        try
        {
            var me = await _authService.SignInAsync(email, password);

            if (me.IsFinanceManager)
            {
                await Shell.Current.GoToAsync("//FinanceQueuePage");
            }
            else
            {
                await Shell.Current.GoToAsync("//EmployeeExpensesPage");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message.Contains("NotAuthorizedException") || ex.Message.Contains("Authentication failed")
                ? "Invalid email or password."
                : $"Error: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool loading)
    {
        SignInButton.IsEnabled = !loading;
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        ErrorLabel.IsVisible = false;
    }
}
