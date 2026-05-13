namespace ExpenseTracker.Maui.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnContinueAsEmployeeClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//EmployeeExpensesPage");

    private async void OnContinueAsFinanceManagerClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//FinanceQueuePage");
}
