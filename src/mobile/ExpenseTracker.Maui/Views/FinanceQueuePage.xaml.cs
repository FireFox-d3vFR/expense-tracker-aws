using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class FinanceQueuePage : ContentPage
{
    private readonly ExpenseApiClient _apiClient = new();

    public FinanceQueuePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        QueueList.ItemsSource = await _apiClient.GetFinanceQueueAsync();
    }
}
