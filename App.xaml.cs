using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Views;

namespace StudentManagementSystem;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;
    public static User? CurrentUser { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogConfig.Initialize();

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            InitializeDatabase();

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            var loginVm = (LoginViewModel)loginWindow.DataContext;
            loginVm.LoginSucceeded += () =>
            {
                CurrentUser = loginVm.AuthenticatedUser;
                Log.Information("User {Username} ({Role}) logged in", CurrentUser?.Username, CurrentUser?.Role);
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                loginWindow.Close();
            };

            loginWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Fatal startup error: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogConfig.Shutdown();
        base.OnExit(e);
    }

    private static void InitializeDatabase()
    {
        try
        {
            using var context = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            MigrateOrBaseline(context);
            DataSeeder.Seed(ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>());
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            CreateDatabase();
            try
            {
                using var context = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                context.Database.Migrate();
                DataSeeder.Seed(ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>());
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }
        catch (Exception ex)
        {
            ShowDbError(ex);
        }
    }

    private static void MigrateOrBaseline(AppDbContext context)
    {
        try
        {
            context.Database.Migrate();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateTable)
        {
            var needsReset = false;

            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";
                var count = (long)cmd.ExecuteScalar()!;
                needsReset = count == 0;
            }
            catch
            {
                needsReset = true;
            }

            if (needsReset)
            {
                Log.Warning("Database created without migrations — dropping and recreating");
                context.Database.EnsureDeleted();
                context.Database.Migrate();
            }
        }
    }

    private static void CreateDatabase()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connString = config.GetConnectionString("DefaultConnection")!;
        var builder = new NpgsqlConnectionStringBuilder(connString);
        var dbName = builder.Database!;
        builder.Database = "postgres";

        using var connection = new NpgsqlConnection(builder.ConnectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        cmd.ExecuteNonQuery();
    }

    private static void ShowDbError(Exception ex)
    {
        var msg = ex switch
        {
            PostgresException pgEx => pgEx.SqlState switch
            {
                PostgresErrorCodes.InvalidPassword => "Invalid PostgreSQL password. Update appsettings.json.",
                PostgresErrorCodes.InvalidCatalogName => $"Database does not exist and could not be created.\n\n{ex.Message}",
                _ => $"PostgreSQL error: {ex.Message}"
            },
            NpgsqlException => $"Cannot connect to PostgreSQL.\n\nEnsure the server is running at the address in appsettings.json.\n\n{ex.Message}",
            _ => $"Database initialization failed.\n\n{ex.Message}"
        };

        MessageBox.Show(msg, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
