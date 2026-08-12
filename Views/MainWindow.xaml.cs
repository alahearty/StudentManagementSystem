using System.Windows;
using System.Windows.Controls;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        var itemsControl = (ItemsControl)NotificationPopup.FindName("NotificationItems");
        itemsControl.ItemsSource = NotificationCenter.Instance.Notifications;
    }

    private async void StudentsTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Students.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadStudentsCommand).ExecuteAsync(null);
    }

    private async void CoursesTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Courses.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadCoursesCommand).ExecuteAsync(null);
    }

    private async void EnrollmentsTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Enrollments.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadEnrollmentsCommand).ExecuteAsync(null);
    }

    private async void AttendanceTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.AttendanceRecords.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadAttendanceCommand).ExecuteAsync(null);
    }

    private async void TimetableTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Schedules.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadSchedulesCommand).ExecuteAsync(null);
    }

    private async void DashboardTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadDashboardCommand).ExecuteAsync(null);
    }

    private void StudentPrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.StudentPageIndex > 0)
            _viewModel.StudentPageIndex--;
    }

    private void StudentNextPage_Click(object sender, RoutedEventArgs e)
    {
        var maxPage = (_viewModel.StudentTotalCount - 1) / _viewModel.StudentPageSize;
        if (_viewModel.StudentPageIndex < maxPage)
            _viewModel.StudentPageIndex++;
    }

    private async void PaymentsTab_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Payments.Count == 0)
            await ((Commands.AsyncRelayCommand)_viewModel.LoadPaymentsCommand).ExecuteAsync(null);
    }

    private void NotificationBell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NotificationCenter.Instance.MarkAllRead();
        NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
    }
}
