using System.Windows;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        PasswordBox.PasswordChanged += (s, e) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        };
    }
}
