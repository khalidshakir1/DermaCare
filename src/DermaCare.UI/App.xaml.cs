using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using DermaCare.UI.ViewModels.Inventory;
using DermaCare.Core.Interfaces;
using DermaCare.Core.Services;
using DermaCare.Data;
using DermaCare.Data.Repositories;
using DermaCare.Reports;
using DermaCare.UI.Helpers;
using DermaCare.UI.ViewModels;
using DermaCare.UI.ViewModels.Appointments;
using DermaCare.UI.ViewModels.Auth;
using DermaCare.UI.ViewModels.Billing;
using DermaCare.UI.ViewModels.Doctors;
using DermaCare.UI.ViewModels.MedicalRecords;
using DermaCare.UI.ViewModels.Patients;
using DermaCare.UI.ViewModels.Reports;
using DermaCare.UI.ViewModels.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DermaCare.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var pkCulture = (CultureInfo)CultureInfo.GetCultureInfo("en-PK").Clone();
        pkCulture.NumberFormat.CurrencySymbol = "Rs. ";
        pkCulture.NumberFormat.CurrencyPositivePattern = 0;
        pkCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        pkCulture.DateTimeFormat.LongDatePattern = "dd MMM yyyy";
        CultureInfo.DefaultThreadCurrentCulture = pkCulture;
        CultureInfo.DefaultThreadCurrentUICulture = pkCulture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(pkCulture.IetfLanguageTag)));

        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DermaCare", "logs", "dermacare-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        Services = services.BuildServiceProvider();

        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DermaDbContext>();
            await DatabaseSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed on startup.");
            MessageBox.Show(
                $"Could not initialize database: {ex.Message}",
                "DermaCare Startup Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");

        services.AddDbContext<DermaDbContext>(options => options.UseSqlServer(connStr));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();

        // Session & Services
        services.AddSingleton<ICurrentSession, CurrentSession>();
        services.AddSingleton<ILicenseService, StubLicenseService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddSingleton<PdfReportExporter>();
        services.AddSingleton<ExcelReportExporter>();

        // Navigation & ViewModels
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PatientListViewModel>();
        services.AddTransient<PatientFormViewModel>();
        services.AddTransient<PatientProfileViewModel>();
        services.AddTransient<DoctorListViewModel>();
        services.AddTransient<DoctorFormViewModel>();
        services.AddTransient<AppointmentCalendarViewModel>();
        services.AddTransient<AppointmentFormViewModel>();
        services.AddTransient<TokenViewModel>();
        services.AddTransient<ConsultationViewModel>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SaleSummaryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<InventoryViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

