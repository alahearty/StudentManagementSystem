using System.Windows;

namespace StudentManagementSystem.Views;

public partial class PasswordResetDialog : Window
{
    public string? NewPassword { get; private set; }

    public PasswordResetDialog()
    {
        InitializeComponent();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordInput.Password))
        {
            MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        NewPassword = PasswordInput.Password;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
