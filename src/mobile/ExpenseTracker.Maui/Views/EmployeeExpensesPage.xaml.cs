using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class EmployeeExpensesPage : ContentPage
{
    private readonly ExpenseApiClient _apiClient = new();

    public EmployeeExpensesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ExpensesList.ItemsSource = await _apiClient.GetMyExpensesAsync();
    }
}
