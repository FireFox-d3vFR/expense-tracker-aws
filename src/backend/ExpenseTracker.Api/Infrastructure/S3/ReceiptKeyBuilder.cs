using System.Globalization;
using System.Text;

namespace ExpenseTracker.Api.Infrastructure.S3;

public static class ReceiptKeyBuilder
{
    private const string Prefix = "receipts";

    public static string Build(string employeeId, string expenseId, string? fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expenseId);

        var safeEmployeeId = SanitizeSegment(employeeId);
        var safeExpenseId = SanitizeSegment(expenseId);
        var safeFileName = SanitizeFileName(fileName);

        return $"{Prefix}/{safeEmployeeId}/{safeExpenseId}/{safeFileName}";
    }

    public static string SanitizeFileName(string? fileName)
    {
        // Normalize Windows backslashes so Path.GetFileName works on Linux/macOS Lambda too.
        var name = Path.GetFileName(fileName?.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(name))
        {
            return $"{Guid.NewGuid():N}.bin";
        }

        return SanitizeSegment(name);
    }

    private static string SanitizeSegment(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var safeCharacter = IsAllowed(character)
                ? char.ToLowerInvariant(character)
                : '-';

            if (safeCharacter is '-')
            {
                if (previousWasSeparator)
                {
                    continue;
                }

                previousWasSeparator = true;
                builder.Append(safeCharacter);
                continue;
            }

            previousWasSeparator = false;
            builder.Append(safeCharacter);
        }

        var sanitized = builder.ToString().Trim('-', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? $"{Guid.NewGuid():N}" : sanitized;
    }

    private static bool IsAllowed(char character) =>
        char.IsAsciiLetterOrDigit(character) ||
        character is '.' or '_' or '-';
}
