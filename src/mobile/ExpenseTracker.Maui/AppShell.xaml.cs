using ExpenseTracker.Maui.Views;

namespace ExpenseTracker.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("CreateExpensePage", typeof(CreateExpensePage));
		Routing.RegisterRoute("ExpenseDetailPage", typeof(ExpenseDetailPage));
	}
}
