using System.Diagnostics;
using System.IO;
using Serilog;
using Serilog.Events;

namespace StudentManagementSystem.Services;

public static class LogConfig
{
    public static void Initialize()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StudentManagementSystem",
            "logs",
            "sms-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "StudentManagementSystem")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        EnableBindingDiagnostics();

        Log.Information("Logging initialized. Logs: {LogPath}", logPath);
    }

    private static void EnableBindingDiagnostics()
    {
        try
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Clear();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new SerilogTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to attach binding diagnostics");
        }
    }

    public static void Shutdown()
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
    }
}
