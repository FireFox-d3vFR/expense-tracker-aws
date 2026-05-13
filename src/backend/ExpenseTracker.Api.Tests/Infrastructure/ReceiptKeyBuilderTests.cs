using ExpenseTracker.Api.Infrastructure.S3;

namespace ExpenseTracker.Api.Tests.Infrastructure;

public sealed class ReceiptKeyBuilderTests
{
    [Fact]
    public void Build_creates_private_receipt_key_with_expected_prefix()
    {
        var key = ReceiptKeyBuilder.Build("employee-1", "expense-1", "receipt.pdf");

        Assert.Equal("receipts/employee-1/expense-1/receipt.pdf", key);
    }

    [Theory]
    [InlineData("../receipt.pdf", "receipt.pdf")]
    [InlineData("..\\receipt.pdf", "receipt.pdf")]
    [InlineData("C:\\temp\\receipt.pdf", "receipt.pdf")]
    [InlineData("/tmp/receipt.pdf", "receipt.pdf")]
    public void Build_removes_path_parts_from_file_name(string fileName, string expectedFileName)
    {
        var key = ReceiptKeyBuilder.Build("employee-1", "expense-1", fileName);

        Assert.EndsWith($"/{expectedFileName}", key);
    }

    [Fact]
    public void Build_normalizes_spaces_accents_and_unsafe_characters()
    {
        var key = ReceiptKeyBuilder.Build(
            "Employee 1",
            "Expense 1",
            "Reçu déjeuner #42!.PDF");

        Assert.Equal("receipts/employee-1/expense-1/recu-dejeuner-42-.pdf", key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_generates_file_name_when_input_is_empty(string? fileName)
    {
        var key = ReceiptKeyBuilder.Build("employee-1", "expense-1", fileName);

        Assert.StartsWith("receipts/employee-1/expense-1/", key);
        Assert.EndsWith(".bin", key);
        Assert.True(key.Length > "receipts/employee-1/expense-1/.bin".Length);
    }

    [Fact]
    public void Build_rejects_empty_employee_id()
    {
        Assert.Throws<ArgumentException>(() =>
            ReceiptKeyBuilder.Build(" ", "expense-1", "receipt.pdf"));
    }

    [Fact]
    public void Build_rejects_empty_expense_id()
    {
        Assert.Throws<ArgumentException>(() =>
            ReceiptKeyBuilder.Build("employee-1", " ", "receipt.pdf"));
    }
}
