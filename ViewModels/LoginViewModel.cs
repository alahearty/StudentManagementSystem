using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels.Base;

namespace StudentManagementSystem.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public LoginViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync());
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set { SetProperty(ref _username, value); Error = string.Empty; }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); Error = string.Empty; }
    }

    private string _error = string.Empty;
    public string Error
    {
        get => _error;
        set => SetProperty(ref _error, value);
    }

    private bool _isLoggingIn;
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set => SetProperty(ref _isLoggingIn, value);
    }

    public ICommand LoginCommand { get; }

    public User? AuthenticatedUser { get; private set; }
    public event Action? LoginSucceeded;

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "Please enter username and password.";
            return;
        }

        IsLoggingIn = true;
        Error = string.Empty;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == Username.Trim() && u.IsActive);

            if (user is null || !AuthService.VerifyPassword(Password, user.PasswordHash))
            {
                Log.Warning("Failed login attempt for username: {Username}", Username.Trim());
                Error = "Invalid username or password.";
                return;
            }

            Log.Information("User {Username} authenticated successfully", user.Username);
            AuthenticatedUser = user;
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login failed for username: {Username}", Username.Trim());
            Error = $"Login failed: {Unwrap(ex)}";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private static string Unwrap(Exception ex)
    {
        var parts = new List<string>();
        var current = ex;
        while (current is not null)
        {
            var msg = current.Message.TrimEnd('.');
            if (parts.Count == 0 || !parts.Last().Contains(msg))
                parts.Add(msg);
            current = current.InnerException;
        }
        return string.Join(" -> ", parts);
    }
}
