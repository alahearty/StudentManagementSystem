using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Views;

namespace StudentManagementSystem;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        InitializeDatabase();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void InitializeDatabase()
    {
        try
        {
            using var context = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            context.Database.EnsureCreated();
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            CreateDatabase();
            try
            {
                using var context = ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                context.Database.EnsureCreated();
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

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
