using System.Windows;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Views;

public partial class TranscriptWindow : Window
{
    private readonly TranscriptViewModel _viewModel;

    public TranscriptWindow(TranscriptViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.CloseRequested = () => Close();
    }

    private async void TranscriptWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadCommand).ExecuteAsync(null);
    }
}
