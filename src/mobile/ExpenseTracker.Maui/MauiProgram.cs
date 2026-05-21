using ExpenseTracker.Maui.Services;
using ExpenseTracker.Maui.Views;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services
		builder.Services.AddSingleton<SecureTokenStore>();
		builder.Services.AddSingleton<HttpClient>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<ExpenseApiClient>();

		// Shell and pages
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<EmployeeExpensesPage>();
		builder.Services.AddTransient<FinanceQueuePage>();
		builder.Services.AddTransient<CreateExpensePage>();
		builder.Services.AddTransient<ExpenseDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
