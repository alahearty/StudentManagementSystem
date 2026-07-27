using System.Windows;
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
}
