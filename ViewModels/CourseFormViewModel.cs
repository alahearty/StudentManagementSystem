using System.Collections.ObjectModel;
using System.Windows.Input;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels.Base;

namespace StudentManagementSystem.ViewModels;

public sealed class CourseFormViewModel : ViewModelBase
{
    private string _courseCode = string.Empty;
    private string _courseName = string.Empty;
    private string _description = string.Empty;
    private string _department = string.Empty;
    private int _credits = 3;
    private int? _prerequisiteCourseId;

    private readonly int? _courseId;

    public CourseFormViewModel(Course? course = null, List<Course>? availableCourses = null)
    {
        SaveCommand = new RelayCommand(_ => SaveRequested?.Invoke(), _ => IsValid);
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke());

        if (availableCourses is not null)
        {
            var filtered = availableCourses.Where(c => c.Id != course?.Id).ToList();
            AvailableCourses = new ObservableCollection<Course>(filtered);
        }

        if (course is not null)
        {
            _courseId = course.Id;
            _courseCode = course.CourseCode;
            _courseName = course.CourseName;
            _description = course.Description ?? string.Empty;
            _department = course.Department ?? string.Empty;
            _credits = course.Credits;
            _prerequisiteCourseId = course.PrerequisiteCourseId;
            SelectedPrerequisiteCourse = AvailableCourses?.FirstOrDefault(c => c.Id == course.PrerequisiteCourseId);
        }
    }

    public ObservableCollection<Course>? AvailableCourses { get; }

    private Course? _selectedPrerequisiteCourse;
    public Course? SelectedPrerequisiteCourse
    {
        get => _selectedPrerequisiteCourse;
        set { SetProperty(ref _selectedPrerequisiteCourse, value); _prerequisiteCourseId = value?.Id; }
    }

    public string CourseCode
    {
        get => _courseCode;
        set { SetProperty(ref _courseCode, value); IsValid = true; Error = string.Empty; }
    }

    public string CourseName
    {
        get => _courseName;
        set { SetProperty(ref _courseName, value); IsValid = true; Error = string.Empty; }
    }

    public string Description
    {
        get => _description;
        set { SetProperty(ref _description, value); IsValid = true; Error = string.Empty; }
    }

    public string Department
    {
        get => _department;
        set { SetProperty(ref _department, value); IsValid = true; Error = string.Empty; }
    }

    public int Credits
    {
        get => _credits;
        set { SetProperty(ref _credits, value); IsValid = true; Error = string.Empty; }
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

    public bool IsEdit => _courseId.HasValue;

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(CourseCode))
            return "Course code is required.";
        if (CourseCode.Trim().Length < 2)
            return "Course code must be at least 2 characters.";
        if (string.IsNullOrWhiteSpace(CourseName))
            return "Course name is required.";
        if (!ValidationHelper.IsValidCredits(Credits))
            return "Credits must be between 1 and 12.";
        return null;
    }

    public Course ToCourse()
    {
        return new Course
        {
            Id = _courseId ?? 0,
            CourseCode = _courseCode.Trim(),
            CourseName = _courseName.Trim(),
            Description = string.IsNullOrWhiteSpace(_description) ? null : _description.Trim(),
            Department = string.IsNullOrWhiteSpace(_department) ? null : _department.Trim(),
            Credits = _credits,
            PrerequisiteCourseId = _prerequisiteCourseId
        };
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? SaveRequested { get; set; }
    public Action? CancelRequested { get; set; }
}
