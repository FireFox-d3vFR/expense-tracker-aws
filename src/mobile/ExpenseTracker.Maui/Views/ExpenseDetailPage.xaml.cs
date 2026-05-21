using ExpenseTracker.Maui.Models;
using ExpenseTracker.Maui.Services;

namespace ExpenseTracker.Maui.Views;

[QueryProperty(nameof(ExpenseId), "expenseId")]
public partial class ExpenseDetailPage : ContentPage
{
    private readonly ExpenseApiClient _apiClient;
    private ExpenseReportDto? _expense;

    public string? ExpenseId { get; set; }

    public ExpenseDetailPage(ExpenseApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrWhiteSpace(ExpenseId))
        {
            await LoadAsync(ExpenseId);
        }
    }

    private async Task LoadAsync(string expenseId)
    {
        Busy.IsVisible = true;
        Busy.IsRunning = true;
        ErrorLabel.IsVisible = false;
        try
        {
            _expense = await _apiClient.GetExpenseByIdAsync(expenseId);
            Render(_expense);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Could not load expense: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            Busy.IsRunning = false;
            Busy.IsVisible = false;
        }
    }

    private void Render(ExpenseReportDto e)
    {
        CategoryLabel.Text = e.Category;
        StatusLabel.Text = e.StatusLabel;
        AmountLabel.Text = e.AmountDisplay;
        DateLabel.Text = e.ExpenseDate.ToString("yyyy-MM-dd");
        DescriptionLabel.Text = e.Description;

        if (e.SubmittedAt.HasValue)
        {
            SubmittedAtLabel.Text = e.SubmittedAt.Value.ToLocalTime().ToString("g");
            SubmittedRow.IsVisible = true;
        }

        if (e.ReviewedAt.HasValue && !string.IsNullOrWhiteSpace(e.ReviewedBy))
        {
            ReviewedByLabel.Text = $"{e.ReviewedBy} — {e.ReviewedAt.Value.ToLocalTime():g}";
            ReviewedRow.IsVisible = true;
        }

        if (!string.IsNullOrWhiteSpace(e.RejectionReason))
        {
            RejectionReasonLabel.Text = e.RejectionReason;
            RejectionRow.IsVisible = true;
        }

        ReceiptLabel.Text = e.HasReceipt ? "Receipt attached" : "No receipt";
        ReceiptRow.IsVisible = true;

        var canSubmit = e.Status is ExpenseStatus.Draft or ExpenseStatus.Rejected;
        SubmitButton.IsVisible = canSubmit;
        UploadReceiptButton.IsVisible = canSubmit;
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (_expense is null) return;
        SubmitButton.IsEnabled = false;
        ErrorLabel.IsVisible = false;
        try
        {
            await _apiClient.SubmitExpenseAsync(_expense.ExpenseId);
            await LoadAsync(_expense.ExpenseId);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Submit failed: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    private async void OnUploadReceiptClicked(object? sender, EventArgs e)
    {
        if (_expense is null) return;

        var fileName = await DisplayPromptAsync(
            "Receipt upload",
            "Enter file name (e.g. receipt.pdf):",
            placeholder: "receipt.pdf");

        if (string.IsNullOrWhiteSpace(fileName)) return;

        var contentType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "image/jpeg";

        UploadReceiptButton.IsEnabled = false;
        ErrorLabel.IsVisible = false;
        try
        {
            var result = await _apiClient.GetReceiptUploadUrlAsync(_expense.ExpenseId, fileName, contentType);
            await Clipboard.SetTextAsync(result.Url);
            await DisplayAlertAsync(
                "Upload URL copied",
                $"The pre-signed upload URL has been copied to your clipboard (expires {result.ExpiresAt.ToLocalTime():g}). Use it with a PUT request to upload your file.",
                "OK");
            await LoadAsync(_expense.ExpenseId);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Could not get upload URL: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            UploadReceiptButton.IsEnabled = true;
        }
    }
}
