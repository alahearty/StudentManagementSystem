using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentManagementSystem.Data;
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
        await ((Commands.AsyncRelayCommand)_viewModel.LoadStudentsCommand).ExecuteAsync(null);
    }

    private async void CoursesTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadCoursesCommand).ExecuteAsync(null);
    }

    private async void EnrollmentsTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadEnrollmentsCommand).ExecuteAsync(null);
    }

    private async void AttendanceTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadAttendanceCommand).ExecuteAsync(null);
    }

    private async void TimetableTab_Loaded(object sender, RoutedEventArgs e)
    {
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
        await ((Commands.AsyncRelayCommand)_viewModel.LoadPaymentsCommand).ExecuteAsync(null);
    }

    private async void UsersTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadUsersCommand).ExecuteAsync(null);
    }

    private async void SemestersTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadSemestersCommand).ExecuteAsync(null);
    }

    private async void ResultsTab_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadResultsCommand).ExecuteAsync(null);
    }

    private void NotificationBell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        NotificationCenter.Instance.MarkAllRead();
        NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
    }

    private async void ExportPaymentsPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var factory = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var pdf = await PdfExporter.ExportPaymentsReportAsync(factory);
            PdfExporter.SavePdf(pdf, "payments_report.pdf");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportAttendancePdf_Click(object sender, RoutedEventArgs e)
    {
        var course = _viewModel.SelectedCourseForAttendance;
        if (course is null)
        {
            MessageBox.Show("Select a course first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var factory = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var pdf = await PdfExporter.ExportAttendanceReportAsync(factory, course.Id);
            PdfExporter.SavePdf(pdf, $"attendance_{course.CourseCode}.pdf");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
