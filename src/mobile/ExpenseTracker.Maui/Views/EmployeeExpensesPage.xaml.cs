using ExpenseTracker.Maui.Models;
using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class EmployeeExpensesPage : ContentPage
{
    private readonly ExpenseApiClient _apiClient;
    private readonly AuthService _authService;

    public EmployeeExpensesPage(ExpenseApiClient apiClient, AuthService authService)
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

        ExpensesList.SelectedItem = null;
        try
        {
            ExpensesList.ItemsSource = await _apiClient.GetMyExpensesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load expenses: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnNewExpenseClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("CreateExpensePage");
    }

    private async void OnExpenseSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ExpenseReportDto selected)
        {
            await Shell.Current.GoToAsync($"ExpenseDetailPage?expenseId={selected.ExpenseId}");
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
