using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class FinanceQueuePage : ContentPage
{
    private readonly ExpenseApiClient _apiClient;
    private readonly AuthService _authService;

    public FinanceQueuePage(ExpenseApiClient apiClient, AuthService authService)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _authService = authService;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _ = LoadAsync(showSpinner: true);
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync(showSpinner: false);
        Refresher.IsRefreshing = false;
    }

    private async Task LoadAsync(bool showSpinner)
    {
        if (showSpinner)
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
        }

        try
        {
            QueueList.ItemsSource = await _apiClient.GetFinanceQueueAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load queue: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnApproveClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string expenseId }) return;

        var confirmed = await DisplayAlertAsync(
            "Approve expense",
            "Are you sure you want to approve this expense?",
            "Approve",
            "Cancel");

        if (confirmed)
        {
            await ReviewAsync(expenseId, "approve");
        }
    }

    private async void OnRejectClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string expenseId }) return;

        var reason = await DisplayPromptAsync(
            "Reject expense",
            "Enter a rejection reason:",
            placeholder: "e.g. Missing receipt");

        if (!string.IsNullOrWhiteSpace(reason))
        {
            await ReviewAsync(expenseId, "reject", reason);
        }
    }

    private async Task ReviewAsync(string expenseId, string decision, string? reason = null)
    {
        try
        {
            await _apiClient.ReviewExpenseAsync(expenseId, decision, reason);
            await LoadAsync(showSpinner: false);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Review failed: {ex.Message}", "OK");
        }
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync("Sign out", "Are you sure you want to sign out?", "Sign out", "Cancel");
        if (confirmed)
        {
            _authService.SignOut();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
