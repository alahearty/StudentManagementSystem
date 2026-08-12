using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudentManagementSystem.Services;

public sealed class NotificationCenter : INotifyPropertyChanged
{
    private static NotificationCenter? _instance;
    public static NotificationCenter Instance => _instance ??= new NotificationCenter();

    public ObservableCollection<Notification> Notifications { get; } = new();

    private int _unreadCount;
    public int UnreadCount
    {
        get => _unreadCount;
        set { _unreadCount = value; OnPropertyChanged(); }
    }

    private bool _hasUnread;
    public bool HasUnread
    {
        get => _hasUnread;
        set { _hasUnread = value; OnPropertyChanged(); }
    }

    public void Add(string message, string type = "info")
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Notifications.Insert(0, new Notification
            {
                Message = message,
                Type = type,
                Timestamp = DateTime.Now
            });

            while (Notifications.Count > 50)
                Notifications.RemoveAt(Notifications.Count - 1);

            UnreadCount = Notifications.Count;
            HasUnread = true;
        });
    }

    public void MarkAllRead()
    {
        UnreadCount = 0;
        HasUnread = false;
    }

    public void Info(string message) => Add(message, "info");
    public void Warn(string message) => Add(message, "warn");
    public void Error(string message) => Add(message, "error");
    public void Success(string message) => Add(message, "success");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class Notification
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public DateTime Timestamp { get; set; }
    public string Icon => Type switch
    {
        "success" => "OK",
        "error" => "!",
        "warn" => "!",
        _ => "i"
    };
}
