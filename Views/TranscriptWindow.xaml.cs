using System.Windows;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Views;

public partial class TranscriptWindow : Window
{
    private readonly TranscriptViewModel _viewModel;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly int _studentId;

    public TranscriptWindow(TranscriptViewModel viewModel, IDbContextFactory<AppDbContext> contextFactory, int studentId)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _contextFactory = contextFactory;
        _studentId = studentId;
        DataContext = _viewModel;

        _viewModel.CloseRequested = () => Close();
    }

    private async void TranscriptWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ((Commands.AsyncRelayCommand)_viewModel.LoadCommand).ExecuteAsync(null);
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pdf = await PdfExporter.ExportTranscriptAsync(_contextFactory, _studentId);
            PdfExporter.SavePdf(pdf, $"transcript_{_viewModel.RegistrationNumber}.pdf");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
