using System.Diagnostics;
using Serilog;

namespace StudentManagementSystem.Services;

public sealed class SerilogTraceListener : TraceListener
{
    public override void Write(string? message)
    {
        Log.Error("WPF Trace: {Message}", message);
    }

    public override void WriteLine(string? message)
    {
        Log.Error("WPF Trace: {Message}", message);
    }
}
