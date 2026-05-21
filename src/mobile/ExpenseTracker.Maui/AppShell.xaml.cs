using ExpenseTracker.Maui.Views;

namespace ExpenseTracker.Maui;

public partial class AppShell : Shell
{
	public AppShell(
		LoginPage loginPage,
		EmployeeExpensesPage employeeExpensesPage,
		FinanceQueuePage financeQueuePage)
	{
		InitializeComponent();
		LoginShellContent.Content = loginPage;
		EmployeeExpensesShellContent.Content = employeeExpensesPage;
		FinanceQueueShellContent.Content = financeQueuePage;
		Routing.RegisterRoute("CreateExpensePage", typeof(CreateExpensePage));
		Routing.RegisterRoute("ExpenseDetailPage", typeof(ExpenseDetailPage));
	}
}
