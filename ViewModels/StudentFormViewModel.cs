using System.ComponentModel;
using System.Windows.Input;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels.Base;

namespace StudentManagementSystem.ViewModels;

public sealed class StudentFormViewModel : ViewModelBase, IDataErrorInfo
{
    private string _registrationNumber = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _department = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private string _gender = string.Empty;
    private DateTime? _dateOfBirth;
    private DateTime? _enrollmentDate;

    private readonly int? _studentId;

    public StudentFormViewModel(Student? student = null)
    {
        SaveCommand = new RelayCommand(_ => SaveRequested?.Invoke(), _ => IsValid);
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke());

        if (student is not null)
        {
            _studentId = student.Id;
            _registrationNumber = student.RegistrationNumber;
            _firstName = student.FirstName;
            _lastName = student.LastName;
            _department = student.Department;
            _email = student.Email;
            _phone = student.Phone ?? string.Empty;
            _address = student.Address ?? string.Empty;
            _gender = student.Gender ?? string.Empty;
            _dateOfBirth = student.DateOfBirth;
            _enrollmentDate = student.EnrollmentDate;
        }
    }

    public string RegistrationNumber
    {
        get => _registrationNumber;
        set { SetProperty(ref _registrationNumber, value); IsValid = true; Error = string.Empty; }
    }

    public string FirstName
    {
        get => _firstName;
        set { SetProperty(ref _firstName, value); IsValid = true; Error = string.Empty; }
    }

    public string LastName
    {
        get => _lastName;
        set { SetProperty(ref _lastName, value); IsValid = true; Error = string.Empty; }
    }

    public string Department
    {
        get => _department;
        set { SetProperty(ref _department, value); IsValid = true; Error = string.Empty; }
    }

    public string Email
    {
        get => _email;
        set { SetProperty(ref _email, value); IsValid = true; Error = string.Empty; }
    }

    public string Phone
    {
        get => _phone;
        set { SetProperty(ref _phone, value); IsValid = true; Error = string.Empty; }
    }

    public string Address
    {
        get => _address;
        set { SetProperty(ref _address, value); IsValid = true; Error = string.Empty; }
    }

    public string Gender
    {
        get => _gender;
        set { SetProperty(ref _gender, value); IsValid = true; Error = string.Empty; }
    }

    public DateTime? DateOfBirth
    {
        get => _dateOfBirth;
        set { SetProperty(ref _dateOfBirth, value); IsValid = true; Error = string.Empty; }
    }

    public DateTime? EnrollmentDate
    {
        get => _enrollmentDate;
        set { SetProperty(ref _enrollmentDate, value); IsValid = true; Error = string.Empty; }
    }

    private bool _isValid = true;
    public bool IsValid
    {
        get => _isValid;
        set { SetProperty(ref _isValid, value); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    private string _error = string.Empty;
    public string Error
    {
        get => _error;
        set { SetProperty(ref _error, value); }
    }

    public bool IsEdit => _studentId.HasValue;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(RegistrationNumber))
            return "Registration number is required.";
        if (RegistrationNumber.Trim().Length < 3)
            return "Registration number must be at least 3 characters.";
        if (string.IsNullOrWhiteSpace(FirstName))
            return "First name is required.";
        if (string.IsNullOrWhiteSpace(LastName))
            return "Last name is required.";
        if (string.IsNullOrWhiteSpace(Department))
            return "Department is required.";
        if (string.IsNullOrWhiteSpace(Email))
            return "Email is required.";
        if (!ValidationHelper.IsValidEmail(Email))
            return "Please enter a valid email address.";
        if (!ValidationHelper.IsValidPhone(Phone))
            return "Please enter a valid phone number.";
        if (DateOfBirth.HasValue && !ValidationHelper.IsValidAge(DateOfBirth))
            return "Age must be between 10 and 120 years.";
        if (!ValidationHelper.IsNotInFuture(DateOfBirth))
            return "Date of birth cannot be in the future.";
        if (!ValidationHelper.IsNotInFuture(EnrollmentDate))
            return "Enrollment date cannot be in the future.";
        return null;
    }

    public Student ToStudent()
    {
        return new Student
        {
            Id = _studentId ?? 0,
            RegistrationNumber = _registrationNumber.Trim(),
            FirstName = _firstName.Trim(),
            LastName = _lastName.Trim(),
            Department = _department.Trim(),
            Email = _email.Trim(),
            Phone = string.IsNullOrWhiteSpace(_phone) ? null : _phone.Trim(),
            Address = string.IsNullOrWhiteSpace(_address) ? null : _address.Trim(),
            Gender = string.IsNullOrWhiteSpace(_gender) ? null : _gender.Trim(),
            DateOfBirth = ToUtc(_dateOfBirth),
            EnrollmentDate = ToUtc(_enrollmentDate)
        };
    }

    private static DateTime? ToUtc(DateTime? dt)
    {
        if (dt is null) return null;
        if (dt.Value.Kind == DateTimeKind.Utc) return dt;
        return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? SaveRequested { get; set; }
    public Action? CancelRequested { get; set; }

    public string this[string columnName]
    {
        get
        {
            return Validate() ?? string.Empty;
        }
    }
}
