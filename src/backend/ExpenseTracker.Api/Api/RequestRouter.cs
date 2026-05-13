using ExpenseTracker.Api.Api.Handlers;

namespace ExpenseTracker.Api.Api;

public sealed class RequestRouter
{
    private static readonly char[] PathSeparators = ['/'];

    public ApiResponse Route(string method, string path, string? body = null)
    {
        var match = Match(method, path);
        if (match is null)
        {
            return ApiResponse.NotFound();
        }

        return match.EndpointName switch
        {
            EndpointNames.Me => MeHandler.Get(match, body),
            EndpointNames.CreateExpense => ExpenseHandlers.Create(match, body),
            EndpointNames.ListEmployeeExpenses => ExpenseHandlers.ListForEmployee(match, body),
            EndpointNames.GetExpense => ExpenseHandlers.GetById(match, body),
            EndpointNames.UpdateExpense => ExpenseHandlers.Update(match, body),
            EndpointNames.SubmitExpense => ExpenseHandlers.Submit(match, body),
            EndpointNames.FinanceQueue => FinanceHandlers.Queue(match, body),
            EndpointNames.ReviewExpense => FinanceHandlers.Review(match, body),
            EndpointNames.ReceiptUrl => ReceiptHandlers.CreateUrl(match, body),
            _ => ApiResponse.NotFound()
        };
    }

    public RouteMatch? Match(string method, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedMethod = method.Trim().ToUpperInvariant();
        var segments = GetSegments(path);

        if (normalizedMethod is "GET" && IsRoute(segments, "me"))
        {
            return RouteMatch.WithoutValues(normalizedMethod, path, EndpointNames.Me);
        }

        if (IsRoute(segments, "expenses"))
        {
            return normalizedMethod switch
            {
                "POST" => RouteMatch.WithoutValues(normalizedMethod, path, EndpointNames.CreateExpense),
                "GET" => RouteMatch.WithoutValues(normalizedMethod, path, EndpointNames.ListEmployeeExpenses),
                _ => null
            };
        }

        if (segments.Length is 2 && segments[0] is "expenses")
        {
            return normalizedMethod switch
            {
                "GET" => ExpenseRoute(normalizedMethod, path, EndpointNames.GetExpense, segments[1]),
                "PUT" => ExpenseRoute(normalizedMethod, path, EndpointNames.UpdateExpense, segments[1]),
                _ => null
            };
        }

        if (normalizedMethod is "POST" &&
            segments.Length is 3 &&
            segments[0] is "expenses" &&
            segments[2] is "submit")
        {
            return ExpenseRoute(normalizedMethod, path, EndpointNames.SubmitExpense, segments[1]);
        }

        if (normalizedMethod is "POST" &&
            segments.Length is 3 &&
            segments[0] is "expenses" &&
            segments[2] is "receipt-url")
        {
            return ExpenseRoute(normalizedMethod, path, EndpointNames.ReceiptUrl, segments[1]);
        }

        if (normalizedMethod is "GET" && IsRoute(segments, "finance", "queue"))
        {
            return RouteMatch.WithoutValues(normalizedMethod, path, EndpointNames.FinanceQueue);
        }

        if (normalizedMethod is "POST" &&
            segments.Length is 4 &&
            segments[0] is "finance" &&
            segments[1] is "expenses" &&
            segments[3] is "review")
        {
            return ExpenseRoute(normalizedMethod, path, EndpointNames.ReviewExpense, segments[2]);
        }

        return null;
    }

    private static RouteMatch ExpenseRoute(
        string method,
        string path,
        string endpointName,
        string expenseId) =>
        new(method, NormalizePath(path), endpointName, new Dictionary<string, string>
        {
            ["expenseId"] = Uri.UnescapeDataString(expenseId)
        });

    private static string[] GetSegments(string path) =>
        NormalizePath(path)
            .Trim('/')
            .Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

    private static string NormalizePath(string path)
    {
        var pathWithoutQuery = path.Split('?', 2)[0];
        var normalized = pathWithoutQuery.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "/" : normalized;
    }

    private static bool IsRoute(string[] segments, params string[] expected) =>
        segments.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase);
}

public static class EndpointNames
{
    public const string Me = "Me.Get";
    public const string CreateExpense = "Expenses.Create";
    public const string ListEmployeeExpenses = "Expenses.ListForEmployee";
    public const string GetExpense = "Expenses.GetById";
    public const string UpdateExpense = "Expenses.Update";
    public const string SubmitExpense = "Expenses.Submit";
    public const string FinanceQueue = "Finance.Queue";
    public const string ReviewExpense = "Finance.Review";
    public const string ReceiptUrl = "Receipts.CreateUrl";
}
