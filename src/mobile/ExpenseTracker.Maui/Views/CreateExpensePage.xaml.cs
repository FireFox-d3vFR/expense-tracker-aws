using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

public partial class CreateExpensePage : ContentPage
{
    private readonly ExpenseApiClient _apiClient;

    public CreateExpensePage(ExpenseApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
        CurrencyPicker.SelectedIndex = 0;
        CategoryPicker.SelectedIndex = 0;
        ExpenseDatePicker.Date = DateTime.Today;
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!decimal.TryParse(AmountEntry.Text?.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            ShowError("Enter a valid positive amount.");
            return;
        }

        var currency = CurrencyPicker.SelectedItem?.ToString();
        var category = CategoryPicker.SelectedItem?.ToString();
        var description = DescriptionEditor.Text?.Trim();

        if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(category))
        {
            ShowError("Select a currency and a category.");
            return;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ShowError("Enter a description.");
            return;
        }

        CreateButton.IsEnabled = false;
        try
        {
            var expenseDate = DateOnly.FromDateTime(ExpenseDatePicker.Date ?? DateTime.Today);
            await _apiClient.CreateExpenseAsync(amount, currency, category, description, expenseDate);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ShowError($"Creation failed: {ex.Message}");
        }
        finally
        {
            CreateButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
