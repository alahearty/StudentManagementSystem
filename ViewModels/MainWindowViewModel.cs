using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.ViewModels.Base;

namespace StudentManagementSystem.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public MainWindowViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        LoadStudentsCommand = new AsyncRelayCommand(async _ => await LoadStudentsAsync());
        SearchStudentsCommand = new RelayCommand(_ => FilterStudents());
        DeleteStudentCommand = new AsyncRelayCommand(async _ => await DeleteStudentAsync(), _ => SelectedStudent is not null);
        OpenAddStudentCommand = new RelayCommand(_ => OpenStudentForm(null));
        OpenEditStudentCommand = new RelayCommand(_ => OpenStudentForm(SelectedStudent), _ => SelectedStudent is not null);

        LoadCoursesCommand = new AsyncRelayCommand(async _ => await LoadCoursesAsync());
        DeleteCourseCommand = new AsyncRelayCommand(async _ => await DeleteCourseAsync(), _ => SelectedCourse is not null);
        OpenAddCourseCommand = new RelayCommand(_ => OpenCourseForm(null));
        OpenEditCourseCommand = new RelayCommand(_ => OpenCourseForm(SelectedCourse), _ => SelectedCourse is not null);

        LoadEnrollmentsCommand = new AsyncRelayCommand(async _ => await LoadEnrollmentsAsync());
        EnrollStudentCommand = new AsyncRelayCommand(async _ => await EnrollStudentAsync(), _ => SelectedStudentForEnroll is not null && SelectedCourseForEnroll is not null);
        RemoveEnrollmentCommand = new AsyncRelayCommand(async _ => await RemoveEnrollmentAsync(), _ => SelectedEnrollment is not null);
        SaveGradeCommand = new AsyncRelayCommand(async _ => await SaveGradeAsync(), _ => SelectedEnrollment is not null);
    }

    #region Students Tab

    private readonly ObservableCollection<Student> _students = new();
    public ObservableCollection<Student> Students => _students;

    private Student? _selectedStudent;
    public Student? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (SetProperty(ref _selectedStudent, value))
            {
                ((AsyncRelayCommand)DeleteStudentCommand).RaiseCanExecuteChanged();
                ((RelayCommand)OpenEditStudentCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ICommand LoadStudentsCommand { get; }
    public ICommand SearchStudentsCommand { get; }
    public ICommand DeleteStudentCommand { get; }
    public ICommand OpenAddStudentCommand { get; }
    public ICommand OpenEditStudentCommand { get; }

    private async Task LoadStudentsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var students = await context.Students.OrderBy(s => s.RegistrationNumber).ToListAsync();
            _students.Clear();
            foreach (var s in students) _students.Add(s);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load students: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterStudents()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _ = LoadStudentsAsync();
            return;
        }

        var text = SearchText.ToLower();
        var filtered = _students.Where(s =>
            s.RegistrationNumber.ToLower().Contains(text) ||
            s.FirstName.ToLower().Contains(text) ||
            s.LastName.ToLower().Contains(text) ||
            s.Department.ToLower().Contains(text) ||
            s.Email.ToLower().Contains(text)).ToList();

        _students.Clear();
        foreach (var s in filtered) _students.Add(s);
    }

    private async Task DeleteStudentAsync()
    {
        if (SelectedStudent is null) return;
        var result = MessageBox.Show(
            $"Delete {SelectedStudent.FullName} ({SelectedStudent.RegistrationNumber})?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var student = await context.Students.FindAsync(SelectedStudent.Id);
            if (student is not null)
            {
                context.Students.Remove(student);
                await context.SaveChangesAsync();
            }
            await LoadStudentsAsync();
            SelectedStudent = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete student: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenStudentForm(Student? student)
    {
        var formVm = new StudentFormViewModel(student);
        formVm.SaveRequested = async () =>
        {
            var validationError = formVm.Validate();
            if (validationError is not null)
            {
                formVm.Error = validationError;
                formVm.IsValid = false;
                return;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var entity = formVm.ToStudent();

                if (formVm.IsEdit)
                {
                    var existing = await context.Students.FindAsync(entity.Id);
                    if (existing is null) return;
                    existing.RegistrationNumber = entity.RegistrationNumber;
                    existing.FirstName = entity.FirstName;
                    existing.LastName = entity.LastName;
                    existing.Department = entity.Department;
                    existing.Email = entity.Email;
                    existing.Phone = entity.Phone;
                    existing.Address = entity.Address;
                    existing.Gender = entity.Gender;
                    existing.DateOfBirth = entity.DateOfBirth;
                    existing.EnrollmentDate = entity.EnrollmentDate;
                }
                else
                {
                    context.Students.Add(entity);
                }

                await context.SaveChangesAsync();
                await LoadStudentsAsync();
                formVm.CancelRequested?.Invoke();
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                formVm.Error = "A student with this registration number already exists.";
                formVm.IsValid = false;
            }
            catch (Exception ex)
            {
                formVm.Error = $"Save failed: {Unwrap(ex)}";
                formVm.IsValid = false;
            }
        };

        formVm.CancelRequested = () => Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == formVm)?.Close();

        var window = new Views.StudentFormWindow { DataContext = formVm, Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    #endregion

    #region Courses Tab

    private readonly ObservableCollection<Course> _courses = new();
    public ObservableCollection<Course> Courses => _courses;

    private Course? _selectedCourse;
    public Course? SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            if (SetProperty(ref _selectedCourse, value))
            {
                ((AsyncRelayCommand)DeleteCourseCommand).RaiseCanExecuteChanged();
                ((RelayCommand)OpenEditCourseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoadCoursesCommand { get; }
    public ICommand DeleteCourseCommand { get; }
    public ICommand OpenAddCourseCommand { get; }
    public ICommand OpenEditCourseCommand { get; }

    private async Task LoadCoursesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var courses = await context.Courses.OrderBy(c => c.CourseCode).ToListAsync();
            _courses.Clear();
            foreach (var c in courses) _courses.Add(c);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load courses: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteCourseAsync()
    {
        if (SelectedCourse is null) return;
        var result = MessageBox.Show(
            $"Delete course {SelectedCourse.CourseCode} - {SelectedCourse.CourseName}? This will also remove related enrollments.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var course = await context.Courses.FindAsync(SelectedCourse.Id);
            if (course is not null)
            {
                context.Courses.Remove(course);
                await context.SaveChangesAsync();
            }
            await LoadCoursesAsync();
            SelectedCourse = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete course: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenCourseForm(Course? course)
    {
        var formVm = new CourseFormViewModel(course);
        formVm.SaveRequested = async () =>
        {
            var validationError = formVm.Validate();
            if (validationError is not null)
            {
                formVm.Error = validationError;
                formVm.IsValid = false;
                return;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var entity = formVm.ToCourse();

                if (formVm.IsEdit)
                {
                    var existing = await context.Courses.FindAsync(entity.Id);
                    if (existing is null) return;
                    existing.CourseCode = entity.CourseCode;
                    existing.CourseName = entity.CourseName;
                    existing.Description = entity.Description;
                    existing.Department = entity.Department;
                    existing.Credits = entity.Credits;
                }
                else
                {
                    context.Courses.Add(entity);
                }

                await context.SaveChangesAsync();
                await LoadCoursesAsync();
                formVm.CancelRequested?.Invoke();
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                formVm.Error = "A course with this code already exists.";
                formVm.IsValid = false;
            }
            catch (Exception ex)
            {
                formVm.Error = $"Save failed: {Unwrap(ex)}";
                formVm.IsValid = false;
            }
        };

        formVm.CancelRequested = () => Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == formVm)?.Close();

        var window = new Views.CourseFormWindow { DataContext = formVm, Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    #endregion

    #region Enrollments Tab

    private readonly ObservableCollection<EnrollmentDisplay> _enrollments = new();
    public ObservableCollection<EnrollmentDisplay> Enrollments => _enrollments;

    private readonly ObservableCollection<Student> _studentsForEnroll = new();
    public ObservableCollection<Student> StudentsForEnroll => _studentsForEnroll;

    private readonly ObservableCollection<Course> _coursesForEnroll = new();
    public ObservableCollection<Course> CoursesForEnroll => _coursesForEnroll;

    private Student? _selectedStudentForEnroll;
    public Student? SelectedStudentForEnroll
    {
        get => _selectedStudentForEnroll;
        set
        {
            if (SetProperty(ref _selectedStudentForEnroll, value))
                ((AsyncRelayCommand)EnrollStudentCommand).RaiseCanExecuteChanged();
        }
    }

    private Course? _selectedCourseForEnroll;
    public Course? SelectedCourseForEnroll
    {
        get => _selectedCourseForEnroll;
        set
        {
            if (SetProperty(ref _selectedCourseForEnroll, value))
                ((AsyncRelayCommand)EnrollStudentCommand).RaiseCanExecuteChanged();
        }
    }

    private string _semester = "2024-Fall";
    public string Semester
    {
        get => _semester;
        set => SetProperty(ref _semester, value);
    }

    private EnrollmentDisplay? _selectedEnrollment;
    public EnrollmentDisplay? SelectedEnrollment
    {
        get => _selectedEnrollment;
        set
        {
            if (SetProperty(ref _selectedEnrollment, value))
            {
                ((AsyncRelayCommand)RemoveEnrollmentCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SaveGradeCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoadEnrollmentsCommand { get; }
    public ICommand EnrollStudentCommand { get; }
    public ICommand RemoveEnrollmentCommand { get; }
    public ICommand SaveGradeCommand { get; }

    private async Task LoadEnrollmentsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollments = await context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .OrderBy(e => e.Semester).ThenBy(e => e.Student.LastName)
                .Select(e => new EnrollmentDisplay
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    CourseId = e.CourseId,
                    StudentName = e.Student.FirstName + " " + e.Student.LastName,
                    RegistrationNumber = e.Student.RegistrationNumber,
                    CourseCode = e.Course.CourseCode,
                    CourseName = e.Course.CourseName,
                    Grade = e.Grade,
                    Semester = e.Semester,
                    EnrollmentDate = e.EnrollmentDate
                })
                .ToListAsync();

            _enrollments.Clear();
            foreach (var e in enrollments) _enrollments.Add(e);

            var students = await context.Students.OrderBy(s => s.RegistrationNumber).ToListAsync();
            _studentsForEnroll.Clear();
            foreach (var s in students) _studentsForEnroll.Add(s);

            var courses = await context.Courses.OrderBy(c => c.CourseCode).ToListAsync();
            _coursesForEnroll.Clear();
            foreach (var c in courses) _coursesForEnroll.Add(c);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load enrollments: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task EnrollStudentAsync()
    {
        if (SelectedStudentForEnroll is null || SelectedCourseForEnroll is null) return;
        if (string.IsNullOrWhiteSpace(Semester))
        {
            MessageBox.Show("Semester is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollment = new Enrollment
            {
                StudentId = SelectedStudentForEnroll.Id,
                CourseId = SelectedCourseForEnroll.Id,
                Semester = Semester.Trim(),
                EnrollmentDate = DateTime.UtcNow
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
            await LoadEnrollmentsAsync();
            SelectedStudentForEnroll = null;
            SelectedCourseForEnroll = null;
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            MessageBox.Show("This student is already enrolled in this course for this semester.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to enroll: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RemoveEnrollmentAsync()
    {
        if (SelectedEnrollment is null) return;
        var result = MessageBox.Show(
            $"Remove enrollment of {SelectedEnrollment.StudentName} in {SelectedEnrollment.CourseCode}?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollment = await context.Enrollments.FindAsync(SelectedEnrollment.EnrollmentId);
            if (enrollment is not null)
            {
                context.Enrollments.Remove(enrollment);
                await context.SaveChangesAsync();
            }
            await LoadEnrollmentsAsync();
            SelectedEnrollment = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to remove enrollment: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveGradeAsync()
    {
        if (SelectedEnrollment is null) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollment = await context.Enrollments.FindAsync(SelectedEnrollment.EnrollmentId);
            if (enrollment is not null)
            {
                enrollment.Grade = SelectedEnrollment.Grade;
                await context.SaveChangesAsync();
                await LoadEnrollmentsAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save grade: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    private static bool IsDuplicateKey(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner is not null)
        {
            if (inner is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
            inner = inner.InnerException;
        }
        return false;
    }

    private static string Unwrap(Exception ex)
    {
        var parts = new List<string>();
        var current = ex;
        while (current is not null)
        {
            var msg = current.Message.TrimEnd('.');
            if (parts.Count == 0 || !parts.Last().Contains(msg))
                parts.Add(msg);
            current = current.InnerException;
        }
        return string.Join(" -> ", parts);
    }
}
