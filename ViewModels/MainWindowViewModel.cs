using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using StudentManagementSystem.Commands;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels.Base;
using StudentManagementSystem.Views;

namespace StudentManagementSystem.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public MainWindowViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        var user = App.CurrentUser;
        IsAdmin = user?.Role == "Admin";
        CurrentUserDisplayName = user?.DisplayName ?? user?.Username ?? "User";
        CurrentUserRole = user?.Role ?? "User";

        LoadStudentsCommand = new AsyncRelayCommand(async _ => await LoadStudentsAsync());
        SearchStudentsCommand = new RelayCommand(_ => FilterStudents());
        DeleteStudentCommand = new AsyncRelayCommand(async _ => await DeleteStudentAsync(), _ => SelectedStudent is not null);
        OpenAddStudentCommand = new RelayCommand(_ => OpenStudentForm(null));
        OpenEditStudentCommand = new RelayCommand(_ => OpenStudentForm(SelectedStudent), _ => SelectedStudent is not null);
        OpenTranscriptCommand = new RelayCommand(_ => OpenTranscript(SelectedStudent), _ => SelectedStudent is not null);

        LoadCoursesCommand = new AsyncRelayCommand(async _ => await LoadCoursesAsync());
        DeleteCourseCommand = new AsyncRelayCommand(async _ => await DeleteCourseAsync(), _ => SelectedCourse is not null);
        OpenAddCourseCommand = new RelayCommand(_ => OpenCourseForm(null));
        OpenEditCourseCommand = new RelayCommand(_ => OpenCourseForm(SelectedCourse), _ => SelectedCourse is not null);

        LoadEnrollmentsCommand = new AsyncRelayCommand(async _ => await LoadEnrollmentsAsync());
        EnrollStudentCommand = new AsyncRelayCommand(async _ => await EnrollStudentAsync(), _ => SelectedStudentForEnroll is not null && SelectedCourseForEnroll is not null);
        RemoveEnrollmentCommand = new AsyncRelayCommand(async _ => await RemoveEnrollmentAsync(), _ => SelectedEnrollment is not null);
        SaveGradeCommand = new AsyncRelayCommand(async _ => await SaveGradeAsync(), _ => SelectedEnrollment is not null);

        LogoutCommand = new RelayCommand(_ => Logout());
        AboutCommand = new RelayCommand(_ => ShowAbout());

        LoadAttendanceCommand = new AsyncRelayCommand(async _ => await LoadAttendanceAsync());
        SaveAttendanceCommand = new AsyncRelayCommand(async _ => await SaveAttendanceAsync());

        LoadSchedulesCommand = new AsyncRelayCommand(async _ => await LoadSchedulesAsync());
        AddScheduleCommand = new AsyncRelayCommand(async _ => await AddScheduleAsync());
        DeleteScheduleCommand = new AsyncRelayCommand(async _ => await DeleteScheduleAsync(), _ => SelectedSchedule is not null);

        LoadDashboardCommand = new AsyncRelayCommand(async _ => await LoadDashboardAsync());
        ExportStudentsCommand = new AsyncRelayCommand(async _ => await ExportStudentsAsync());
        ExportEnrollmentsCommand = new AsyncRelayCommand(async _ => await ExportEnrollmentsAsync());
        ImportStudentsCommand = new RelayCommand(_ => ImportStudents());
        ImportCoursesCommand = new RelayCommand(_ => ImportCourses());
        SeedSampleDataCommand = new AsyncRelayCommand(async _ => await SeedSampleDataAsync());

        LoadUsersCommand = new AsyncRelayCommand(async _ => await LoadUsersAsync());
        AddUserCommand = new AsyncRelayCommand(async _ => await AddUserAsync());
        DeleteUserCommand = new AsyncRelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser is not null);
        ResetPasswordCommand = new AsyncRelayCommand(async _ => await ResetPasswordAsync(), _ => SelectedUser is not null);

        LoadPaymentsCommand = new AsyncRelayCommand(async _ => await LoadPaymentsAsync());
        AddPaymentCommand = new AsyncRelayCommand(async _ => await AddPaymentAsync());
        DeletePaymentCommand = new AsyncRelayCommand(async _ => await DeletePaymentAsync(), _ => SelectedPayment is not null);

        LoadSemestersCommand = new AsyncRelayCommand(async _ => await LoadSemestersAsync());
        AddSemesterCommand = new AsyncRelayCommand(async _ => await AddSemesterAsync());
        DeleteSemesterCommand = new AsyncRelayCommand(async _ => await DeleteSemesterAsync(), _ => SelectedSemester is not null);

        LoadResultsCommand = new AsyncRelayCommand(async _ => await LoadResultsAsync());
        SaveScoresCommand = new AsyncRelayCommand(async _ => await SaveScoresAsync());
        PublishResultsCommand = new AsyncRelayCommand(async _ => await PublishResultsAsync());
    }

    public bool IsAdmin { get; }
    public string CurrentUserDisplayName { get; }
    public string CurrentUserRole { get; }
    public ICommand LogoutCommand { get; }
    public ICommand AboutCommand { get; }

    private void Logout()
    {
        var result = MessageBox.Show("Sign out and return to the login screen?", "Confirm Sign Out",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        App.CurrentUser = null;
        var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is not LoginWindow)?.Close();
    }

    private void ShowAbout()
    {
        var dialog = new AboutDialog { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
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
                ((RelayCommand)OpenTranscriptCommand).RaiseCanExecuteChanged();
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
    public ICommand OpenTranscriptCommand { get; }

    private int _studentPageSize = 50;
    public int StudentPageSize
    {
        get => _studentPageSize;
        set { SetProperty(ref _studentPageSize, value); _ = LoadStudentsAsync(); }
    }

    private int _studentPageIndex;
    public int StudentPageIndex
    {
        get => _studentPageIndex;
        set { SetProperty(ref _studentPageIndex, value); _ = LoadStudentsAsync(); }
    }

    private int _studentTotalCount;
    public int StudentTotalCount
    {
        get => _studentTotalCount;
        set => SetProperty(ref _studentTotalCount, value);
    }

    public string StudentPageInfo =>
        $"Showing {_studentPageIndex * _studentPageSize + 1}-{Math.Min((_studentPageIndex + 1) * _studentPageSize, _studentTotalCount)} of {_studentTotalCount}";

    public List<int> PageSizes { get; } = new() { 25, 50, 100, 200 };

    private async Task LoadStudentsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            IQueryable<Student> query = context.Students;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var text = SearchText.ToLower();
                query = query.Where(s =>
                    s.RegistrationNumber.ToLower().Contains(text) ||
                    s.FirstName.ToLower().Contains(text) ||
                    s.LastName.ToLower().Contains(text) ||
                    s.Department.ToLower().Contains(text) ||
                    s.Email.ToLower().Contains(text));
            }

            StudentTotalCount = await query.CountAsync();
            var students = await query
                .OrderBy(s => s.RegistrationNumber)
                .Skip(StudentPageIndex * StudentPageSize)
                .Take(StudentPageSize)
                .ToListAsync();
            _students.Clear();
            foreach (var s in students) _students.Add(s);
            OnPropertyChanged(nameof(StudentPageInfo));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load students");
            MessageBox.Show($"Failed to load students: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterStudents()
    {
        StudentPageIndex = 0;
        _ = LoadStudentsAsync();
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
            NotificationCenter.Instance.Warn($"Student {student?.FullName} deleted.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete student {StudentId}", SelectedStudent?.Id);
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
                NotificationCenter.Instance.Success(formVm.IsEdit ? "Student updated successfully." : "Student registered successfully.");
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

    private void OpenTranscript(Student? student)
    {
        if (student is null) return;
        var transcriptVm = new TranscriptViewModel(_contextFactory, student);
        var window = new TranscriptWindow(transcriptVm, _contextFactory, student.Id) { Owner = Application.Current.MainWindow };
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
            var courses = await context.Courses.Include(c => c.PrerequisiteCourse).OrderBy(c => c.CourseCode).ToListAsync();
            _courses.Clear();
            foreach (var c in courses) _courses.Add(c);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load courses");
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
            Log.Error(ex, "Failed to delete course {CourseId}", SelectedCourse?.Id);
            MessageBox.Show($"Failed to delete course: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenCourseForm(Course? course)
    {
        var formVm = new CourseFormViewModel(course, _courses.ToList());
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
                    existing.PrerequisiteCourseId = entity.PrerequisiteCourseId;
                }
                else
                {
                    context.Courses.Add(entity);
                }

                await context.SaveChangesAsync();
                await LoadCoursesAsync();
                formVm.CancelRequested?.Invoke();
                NotificationCenter.Instance.Success(formVm.IsEdit ? "Course updated successfully." : "Course added successfully.");
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

    private DateTime? _enrollmentFilterFrom;
    public DateTime? EnrollmentFilterFrom
    {
        get => _enrollmentFilterFrom;
        set { SetProperty(ref _enrollmentFilterFrom, value); _ = LoadEnrollmentsAsync(); }
    }

    private DateTime? _enrollmentFilterTo;
    public DateTime? EnrollmentFilterTo
    {
        get => _enrollmentFilterTo;
        set { SetProperty(ref _enrollmentFilterTo, value); _ = LoadEnrollmentsAsync(); }
    }

    private string _enrollmentFilterSemesterAll = "All";
    public string EnrollmentFilterSemesterAll
    {
        get => _enrollmentFilterSemesterAll;
        set { SetProperty(ref _enrollmentFilterSemesterAll, value); _ = LoadEnrollmentsAsync(); }
    }

    private string _enrollmentStatusFilter = "All";
    public string EnrollmentStatusFilter
    {
        get => _enrollmentStatusFilter;
        set { SetProperty(ref _enrollmentStatusFilter, value); _ = LoadEnrollmentsAsync(); }
    }

    public List<string> GradeStatusFilters { get; } = new() { "All", "Graded", "Ungraded", "Passed", "Failed" };

    private ObservableCollection<string> _semesterList = new();
    public ObservableCollection<string> SemesterList { get => _semesterList; set => SetProperty(ref _semesterList, value); }

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
            IQueryable<Enrollment> query = context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course);

            if (EnrollmentFilterFrom.HasValue)
                query = query.Where(e => e.EnrollmentDate >= EnrollmentFilterFrom.Value);
            if (EnrollmentFilterTo.HasValue)
                query = query.Where(e => e.EnrollmentDate <= EnrollmentFilterTo.Value);
            if (EnrollmentFilterSemesterAll != "All" && !string.IsNullOrWhiteSpace(EnrollmentFilterSemesterAll))
                query = query.Where(e => e.Semester == EnrollmentFilterSemesterAll);
            if (EnrollmentStatusFilter == "Graded")
                query = query.Where(e => e.Grade != null);
            else if (EnrollmentStatusFilter == "Ungraded")
                query = query.Where(e => e.Grade == null);
            else if (EnrollmentStatusFilter == "Passed")
                query = query.Where(e => e.Grade != null && e.Grade != "F");
            else if (EnrollmentStatusFilter == "Failed")
                query = query.Where(e => e.Grade == "F");

            var enrollments = await query
                .OrderBy(e => e.Semester).ThenBy(e => e.Student.LastName)
                .Take(1000)
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

            await LoadLookupDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load enrollments");
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

            var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == SelectedCourseForEnroll.Id);
            if (course?.PrerequisiteCourseId is not null)
            {
                var prereqPassed = await context.Enrollments.AnyAsync(e =>
                    e.StudentId == SelectedStudentForEnroll.Id &&
                    e.CourseId == course.PrerequisiteCourseId.Value &&
                    e.Grade != null &&
                    e.Grade != "F");
                if (!prereqPassed)
                {
                    var prereq = await context.Courses.FindAsync(course.PrerequisiteCourseId.Value);
                    MessageBox.Show(
                        $"Prerequisite not met: {prereq?.CourseCode} must be completed with a passing grade.",
                        "Prerequisite Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var enrollment = new Enrollment
            {
                StudentId = SelectedStudentForEnroll.Id,
                CourseId = SelectedCourseForEnroll.Id,
                Semester = Semester.Trim(),
                EnrollmentDate = DateTime.UtcNow
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
            var courseCode = SelectedCourseForEnroll.CourseCode;
            await LoadEnrollmentsAsync();
            SelectedStudentForEnroll = null;
            SelectedCourseForEnroll = null;
            NotificationCenter.Instance.Success($"Student enrolled in {courseCode}.");
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            MessageBox.Show("This student is already enrolled in this course for this semester.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to enroll student {StudentId} in course {CourseId}", SelectedStudentForEnroll?.Id, SelectedCourseForEnroll?.Id);
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
            Log.Error(ex, "Failed to remove enrollment {EnrollmentId}", SelectedEnrollment?.EnrollmentId);
            MessageBox.Show($"Failed to remove enrollment: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveGradeAsync()
    {
        if (SelectedEnrollment is null) return;

        if (!string.IsNullOrWhiteSpace(SelectedEnrollment.Grade) && ResultComputationEngine.GradePointFromGrade(SelectedEnrollment.Grade) is null)
        {
            MessageBox.Show($"Invalid grade. Use {string.Join(", ", ResultComputationEngine.GradeBands.Select(b => b.Grade))}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollment = await context.Enrollments.FindAsync(SelectedEnrollment.EnrollmentId);
            if (enrollment is not null)
            {
                if (enrollment.IsResultPublished)
                {
                    MessageBox.Show("This result is published and cannot be modified. Use the Results tab workflow.",
                        "Published", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                enrollment.Grade = SelectedEnrollment.Grade;
                await context.SaveChangesAsync();
                await LoadEnrollmentsAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save grade for enrollment {EnrollmentId}", SelectedEnrollment?.EnrollmentId);
            MessageBox.Show($"Failed to save grade: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Attendance Tab

    private readonly ObservableCollection<AttendanceRecord> _attendanceRecords = new();
    public ObservableCollection<AttendanceRecord> AttendanceRecords => _attendanceRecords;

    private readonly ObservableCollection<AttendanceEntry> _attendanceEntries = new();
    public ObservableCollection<AttendanceEntry> AttendanceEntries => _attendanceEntries;

    private Course? _selectedCourseForAttendance;
    public Course? SelectedCourseForAttendance
    {
        get => _selectedCourseForAttendance;
        set { SetProperty(ref _selectedCourseForAttendance, value); _ = LoadAttendanceEntriesAsync(); }
    }

    private DateTime _attendanceDate = DateTime.Today;
    public DateTime AttendanceDate
    {
        get => _attendanceDate;
        set { SetProperty(ref _attendanceDate, value); _ = LoadAttendanceEntriesAsync(); }
    }

    public ICommand LoadAttendanceCommand { get; }
    public ICommand SaveAttendanceCommand { get; }

    private async Task LoadAttendanceAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var records = await context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Student.LastName)
                .Take(500)
                .Select(a => new AttendanceRecord
                {
                    Id = a.Id,
                    StudentName = a.Student.FirstName + " " + a.Student.LastName,
                    RegistrationNumber = a.Student.RegistrationNumber,
                    CourseCode = a.Course.CourseCode,
                    CourseName = a.Course.CourseName,
                    Date = a.Date,
                    Status = a.Status,
                    Remarks = a.Remarks
                })
                .ToListAsync();

            _attendanceRecords.Clear();
            foreach (var r in records) _attendanceRecords.Add(r);

            await LoadLookupDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load attendance");
            MessageBox.Show($"Failed to load attendance: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAttendanceEntriesAsync()
    {
        _attendanceEntries.Clear();
        if (SelectedCourseForAttendance is null) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollments = await context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == SelectedCourseForAttendance.Id)
                .Select(e => new { e.Student, e.Id })
                .ToListAsync();

            var existingRecords = await context.Attendances
                .Where(a => a.CourseId == SelectedCourseForAttendance.Id && a.Date.Date == AttendanceDate.Date)
                .ToDictionaryAsync(a => a.StudentId);

            foreach (var e in enrollments)
            {
                var status = existingRecords.TryGetValue(e.Student.Id, out var record)
                    ? record.Status : "Present";
                var remarks = existingRecords.TryGetValue(e.Student.Id, out var r)
                    ? r.Remarks : null;

                _attendanceEntries.Add(new AttendanceEntry
                {
                    StudentId = e.Student.Id,
                    StudentName = e.Student.FullName,
                    RegistrationNumber = e.Student.RegistrationNumber,
                    Status = status,
                    Remarks = remarks ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load attendance entries");
        }
    }

    private async Task SaveAttendanceAsync()
    {
        if (SelectedCourseForAttendance is null) return;
        if (_attendanceEntries.Count == 0)
        {
            MessageBox.Show("No students enrolled in this course.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var date = AttendanceDate.Date;
            var courseId = SelectedCourseForAttendance.Id;

            var existing = await context.Attendances
                .Where(a => a.CourseId == courseId && a.Date.Date == date)
                .ToListAsync();
            context.Attendances.RemoveRange(existing);

            foreach (var entry in _attendanceEntries)
            {
                context.Attendances.Add(new Attendance
                {
                    StudentId = entry.StudentId,
                    CourseId = courseId,
                    Date = date,
                    Status = entry.Status,
                    Remarks = string.IsNullOrWhiteSpace(entry.Remarks) ? null : entry.Remarks.Trim()
                });
            }

            await context.SaveChangesAsync();
            await LoadAttendanceAsync();
            await LoadAttendanceEntriesAsync();
            MessageBox.Show($"Attendance saved for {date:yyyy-MM-dd}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save attendance");
            MessageBox.Show($"Failed to save attendance: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Timetable Tab

    private readonly ObservableCollection<Schedule> _schedules = new();
    public ObservableCollection<Schedule> Schedules => _schedules;

    private readonly ObservableCollection<Course> _scheduleCourses = new();
    public ObservableCollection<Course> ScheduleCourses => _scheduleCourses;

    private Schedule? _selectedSchedule;
    public Schedule? SelectedSchedule
    {
        get => _selectedSchedule;
        set
        {
            if (SetProperty(ref _selectedSchedule, value))
                ((AsyncRelayCommand)DeleteScheduleCommand).RaiseCanExecuteChanged();
        }
    }

    private Course? _selectedScheduleCourse;
    public Course? SelectedScheduleCourse
    {
        get => _selectedScheduleCourse;
        set => SetProperty(ref _selectedScheduleCourse, value);
    }

    private string _scheduleDay = "Monday";
    public string ScheduleDay
    {
        get => _scheduleDay;
        set => SetProperty(ref _scheduleDay, value);
    }

    private string _scheduleStartTime = "09:00";
    public string ScheduleStartTime
    {
        get => _scheduleStartTime;
        set => SetProperty(ref _scheduleStartTime, value);
    }

    private string _scheduleEndTime = "10:30";
    public string ScheduleEndTime
    {
        get => _scheduleEndTime;
        set => SetProperty(ref _scheduleEndTime, value);
    }

    private string _scheduleRoom = string.Empty;
    public string ScheduleRoom
    {
        get => _scheduleRoom;
        set => SetProperty(ref _scheduleRoom, value);
    }

    private string _scheduleInstructor = string.Empty;
    public string ScheduleInstructor
    {
        get => _scheduleInstructor;
        set => SetProperty(ref _scheduleInstructor, value);
    }

    private string _scheduleFilterDay = "All";
    public string ScheduleFilterDay
    {
        get => _scheduleFilterDay;
        set { SetProperty(ref _scheduleFilterDay, value); FilterSchedules(); }
    }

    private readonly List<Schedule> _allSchedules = new();

    public ICommand LoadSchedulesCommand { get; }
    public ICommand AddScheduleCommand { get; }
    public ICommand DeleteScheduleCommand { get; }

    public List<string> Days { get; } = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    public List<string> FilterDays { get; } = new() { "All", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private async Task LoadSchedulesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var schedules = await context.Schedules
                .Include(s => s.Course)
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .ToListAsync();

            _allSchedules.Clear();
            _allSchedules.AddRange(schedules);

            await LoadLookupDataAsync();

            FilterSchedules();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load schedules");
            MessageBox.Show($"Failed to load schedules: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterSchedules()
    {
        var filtered = ScheduleFilterDay == "All"
            ? _allSchedules
            : _allSchedules.Where(s => s.DayOfWeek == ScheduleFilterDay).ToList();

        _schedules.Clear();
        foreach (var s in filtered) _schedules.Add(s);
    }

    private async Task AddScheduleAsync()
    {
        if (SelectedScheduleCourse is null)
        {
            MessageBox.Show("Please select a course.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TimeSpan.TryParse(ScheduleStartTime, out var start) || !TimeSpan.TryParse(ScheduleEndTime, out var end))
        {
            MessageBox.Show("Invalid time format. Use HH:mm.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (end <= start)
        {
            MessageBox.Show("End time must be after start time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Schedules.Add(new Schedule
            {
                CourseId = SelectedScheduleCourse.Id,
                DayOfWeek = ScheduleDay,
                StartTime = start,
                EndTime = end,
                Room = string.IsNullOrWhiteSpace(ScheduleRoom) ? null : ScheduleRoom.Trim(),
                Instructor = string.IsNullOrWhiteSpace(ScheduleInstructor) ? null : ScheduleInstructor.Trim()
            });

            await context.SaveChangesAsync();
            await LoadSchedulesAsync();
            SelectedScheduleCourse = null;
            ScheduleRoom = string.Empty;
            ScheduleInstructor = string.Empty;
            Log.Information("Schedule added: {Course}, {Day}", SelectedScheduleCourse?.CourseCode, ScheduleDay);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add schedule");
            MessageBox.Show($"Failed to add schedule: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteScheduleAsync()
    {
        if (SelectedSchedule is null) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var schedule = await context.Schedules.FindAsync(SelectedSchedule.Id);
            if (schedule is not null)
            {
                context.Schedules.Remove(schedule);
                await context.SaveChangesAsync();
            }
            await LoadSchedulesAsync();
            SelectedSchedule = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete schedule {ScheduleId}", SelectedSchedule?.Id);
            MessageBox.Show($"Failed to delete schedule: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Dashboard Tab

    private int _dashboardTotalStudents;
    public int DashboardTotalStudents { get => _dashboardTotalStudents; set => SetProperty(ref _dashboardTotalStudents, value); }

    private int _dashboardTotalCourses;
    public int DashboardTotalCourses { get => _dashboardTotalCourses; set => SetProperty(ref _dashboardTotalCourses, value); }

    private int _dashboardTotalEnrollments;
    public int DashboardTotalEnrollments { get => _dashboardTotalEnrollments; set => SetProperty(ref _dashboardTotalEnrollments, value); }

    private double _dashboardAvgGpa;
    public double DashboardAvgGpa { get => _dashboardAvgGpa; set => SetProperty(ref _dashboardAvgGpa, value); }

    private int _dashboardActiveSemesters;
    public int DashboardActiveSemesters { get => _dashboardActiveSemesters; set => SetProperty(ref _dashboardActiveSemesters, value); }

    private string _dashboardTopDept = string.Empty;
    public string DashboardTopDept { get => _dashboardTopDept; set => SetProperty(ref _dashboardTopDept, value); }

    private string _dashboardTopDeptCount = string.Empty;
    public string DashboardTopDeptCount { get => _dashboardTopDeptCount; set => SetProperty(ref _dashboardTopDeptCount, value); }

    public ICommand LoadDashboardCommand { get; }
    public ICommand ExportStudentsCommand { get; }
    public ICommand ExportEnrollmentsCommand { get; }
    public ICommand ImportStudentsCommand { get; }
    public ICommand ImportCoursesCommand { get; }
    public ICommand SeedSampleDataCommand { get; }

    private async Task SeedSampleDataAsync()
    {
        var result = MessageBox.Show(
            "Seed the database with sample students, courses, enrollments, schedules and payments?\nThis only works on an empty database.",
            "Seed Sample Data", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var stats = DataSeeder.SeedSampleData(_contextFactory);
            if (stats.Students == 0)
            {
                MessageBox.Show("Database already contains data. Nothing was seeded.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NotificationCenter.Instance.Success($"Sample data seeded: {stats.Students} students, {stats.Courses} courses, {stats.Enrollments} enrollments.");
            await LoadDashboardAsync();
            await LoadStudentsAsync();
            await LoadCoursesAsync();
            await LoadLookupDataAsync();
            await LoadEnrollmentsAsync();
            await LoadAttendanceAsync();
            await LoadSchedulesAsync();
            await LoadPaymentsAsync();
            await LoadResultsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to seed sample data");
            MessageBox.Show($"Seeding failed: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private readonly ObservableCollection<DepartmentStat> _departmentStats = new();
    public ObservableCollection<DepartmentStat> DepartmentStats => _departmentStats;

    private void ImportStudents()
    {
        var result = CsvImporter.OpenAndImport(CsvImporter.ImportStudentsAsync, _contextFactory, "Students");
        if (result.Item1 > 0)
        {
            _ = LoadStudentsAsync();
            NotificationCenter.Instance.Success($"Imported {result.Item1} students.");
        }
    }

    private void ImportCourses()
    {
        var result = CsvImporter.OpenAndImport(CsvImporter.ImportCoursesAsync, _contextFactory, "Courses");
        if (result.Item1 > 0)
        {
            _ = LoadCoursesAsync();
            NotificationCenter.Instance.Success($"Imported {result.Item1} courses.");
        }
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            DashboardTotalStudents = await context.Students.CountAsync();
            DashboardTotalCourses = await context.Courses.CountAsync();
            DashboardTotalEnrollments = await context.Enrollments.CountAsync();
            DashboardActiveSemesters = await context.Enrollments.Select(e => e.Semester).Distinct().CountAsync();

            var topDept = await context.Students
                .GroupBy(s => s.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (topDept is not null)
            {
                DashboardTopDept = topDept.Dept;
                DashboardTopDeptCount = $"{topDept.Count} students";
            }

            var deptStats = await context.Students
                .GroupBy(s => s.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToListAsync();

            var maxCount = deptStats.Count > 0 ? deptStats.Max(d => d.Count) : 1;
            _departmentStats.Clear();
            foreach (var d in deptStats)
            {
                _departmentStats.Add(new DepartmentStat
                {
                    Department = d.Dept,
                    Count = d.Count,
                    Percentage = (int)Math.Round(d.Count * 100.0 / maxCount)
                });
            }

            var graded = await context.Enrollments
                .Where(e => e.Grade != null && e.Grade != "F")
                .ToListAsync();
            if (graded.Count > 0)
            {
                decimal totalGp = 0;
                foreach (var g in graded)
                {
                    totalGp += ResultComputationEngine.GradePointFromGrade(g.Grade) ?? 0;
                }
                DashboardAvgGpa = Math.Round((double)(totalGp / graded.Count), 2);
            }
            else
            {
                DashboardAvgGpa = 0;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dashboard");
        }
    }

    private async Task ExportStudentsAsync()
    {
        try
        {
            var csv = await CsvExporter.ExportStudentsAsync(_contextFactory);
            CsvExporter.SaveFile(csv, "students.csv");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export students");
            MessageBox.Show($"Export failed: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportEnrollmentsAsync()
    {
        try
        {
            var csv = await CsvExporter.ExportEnrollmentsAsync(_contextFactory);
            CsvExporter.SaveFile(csv, "enrollments.csv");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export enrollments");
            MessageBox.Show($"Export failed: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Users Tab

    private readonly ObservableCollection<User> _users = new();
    public ObservableCollection<User> Users => _users;

    private User? _selectedUser;
    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                ((AsyncRelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)ResetPasswordCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string _newUsername = string.Empty;
    public string NewUsername { get => _newUsername; set => SetProperty(ref _newUsername, value); }

    private string _newPassword = string.Empty;
    public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

    private string _newDisplayName = string.Empty;
    public string NewDisplayName { get => _newDisplayName; set => SetProperty(ref _newDisplayName, value); }

    private string _newRole = "Teacher";
    public string NewRole { get => _newRole; set => SetProperty(ref _newRole, value); }

    private bool _newIsActive = true;
    public bool NewIsActive { get => _newIsActive; set => SetProperty(ref _newIsActive, value); }

    public List<string> Roles { get; } = new() { "Admin", "Teacher" };

    public ICommand LoadUsersCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand ResetPasswordCommand { get; }

    private async Task LoadUsersAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var users = await context.Users.OrderBy(u => u.Username).ToListAsync();
            _users.Clear();
            foreach (var u in users) _users.Add(u);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load users");
            MessageBox.Show($"Failed to load users: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            MessageBox.Show("Username and password are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var exists = await context.Users.AnyAsync(u => u.Username == NewUsername.Trim());
            if (exists)
            {
                MessageBox.Show("Username already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            context.Users.Add(new User
            {
                Username = NewUsername.Trim(),
                PasswordHash = Services.AuthService.HashPassword(NewPassword),
                Role = NewRole,
                DisplayName = string.IsNullOrWhiteSpace(NewDisplayName) ? NewUsername.Trim() : NewDisplayName.Trim(),
                IsActive = NewIsActive,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            await LoadUsersAsync();
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            NewDisplayName = string.Empty;
            NewRole = "Teacher";
            NewIsActive = true;
            NotificationCenter.Instance.Success($"User '{NewUsername}' created.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create user");
            MessageBox.Show($"Failed to create user: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedUser is null) return;
        if (SelectedUser.Username == App.CurrentUser?.Username)
        {
            MessageBox.Show("You cannot delete your own account.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Delete user '{SelectedUser.Username}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(SelectedUser.Id);
            if (user is not null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
            await LoadUsersAsync();
            SelectedUser = null;
            NotificationCenter.Instance.Warn($"User deleted.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete user {UserId}", SelectedUser?.Id);
            MessageBox.Show($"Failed to delete user: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null) return;

        var dialog = new Views.PasswordResetDialog { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.NewPassword))
            return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(SelectedUser.Id);
            if (user is not null)
            {
                user.PasswordHash = Services.AuthService.HashPassword(dialog.NewPassword);
                await context.SaveChangesAsync();
                NotificationCenter.Instance.Success($"Password reset for '{user.Username}'.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reset password for {UserId}", SelectedUser?.Id);
            MessageBox.Show($"Failed to reset password: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Semesters Tab

    private readonly ObservableCollection<Semester> _semesters = new();
    public ObservableCollection<Semester> Semesters => _semesters;

    private Semester? _selectedSemester;
    public Semester? SelectedSemester
    {
        get => _selectedSemester;
        set
        {
            if (SetProperty(ref _selectedSemester, value))
                ((AsyncRelayCommand)DeleteSemesterCommand).RaiseCanExecuteChanged();
        }
    }

    private string _semesterName = string.Empty;
    public string SemesterName { get => _semesterName; set => SetProperty(ref _semesterName, value); }

    private DateTime _semesterStart = DateTime.Today;
    public DateTime SemesterStart { get => _semesterStart; set => SetProperty(ref _semesterStart, value); }

    private DateTime _semesterEnd = DateTime.Today.AddMonths(4);
    public DateTime SemesterEnd { get => _semesterEnd; set => SetProperty(ref _semesterEnd, value); }

    private bool _semesterIsActive = true;
    public bool SemesterIsActive { get => _semesterIsActive; set => SetProperty(ref _semesterIsActive, value); }

    public ICommand LoadSemestersCommand { get; }
    public ICommand AddSemesterCommand { get; }
    public ICommand DeleteSemesterCommand { get; }

    private async Task LoadSemestersAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var semesters = await context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync();
            _semesters.Clear();
            foreach (var s in semesters) _semesters.Add(s);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load semesters");
            MessageBox.Show($"Failed to load semesters: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AddSemesterAsync()
    {
        if (string.IsNullOrWhiteSpace(SemesterName))
        {
            MessageBox.Show("Semester name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SemesterEnd <= SemesterStart)
        {
            MessageBox.Show("End date must be after start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var exists = await context.Semesters.AnyAsync(s => s.Name == SemesterName.Trim());
            if (exists)
            {
                MessageBox.Show("Semester already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            context.Semesters.Add(new Semester
            {
                Name = SemesterName.Trim(),
                StartDate = SemesterStart,
                EndDate = SemesterEnd,
                IsActive = SemesterIsActive
            });

            await context.SaveChangesAsync();
            await LoadSemestersAsync();
            SemesterName = string.Empty;
            NotificationCenter.Instance.Success("Semester created.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create semester");
            MessageBox.Show($"Failed to create semester: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteSemesterAsync()
    {
        if (SelectedSemester is null) return;
        var result = MessageBox.Show($"Delete semester '{SelectedSemester.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var s = await context.Semesters.FindAsync(SelectedSemester.Id);
            if (s is not null) { context.Semesters.Remove(s); await context.SaveChangesAsync(); }
            await LoadSemestersAsync();
            SelectedSemester = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete semester");
            MessageBox.Show($"Failed to delete semester: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Results Tab

    private readonly ObservableCollection<ResultEntry> _resultEntries = new();
    public ObservableCollection<ResultEntry> ResultEntries => _resultEntries;

    private Course? _selectedCourseForResults;
    public Course? SelectedCourseForResults
    {
        get => _selectedCourseForResults;
        set { SetProperty(ref _selectedCourseForResults, value); _ = LoadResultEntriesAsync(); }
    }

    private string _resultSemester = string.Empty;
    public string ResultSemester
    {
        get => _resultSemester;
        set { SetProperty(ref _resultSemester, value); _ = LoadResultEntriesAsync(); }
    }

    private string _resultSummary = string.Empty;
    public string ResultSummary
    {
        get => _resultSummary;
        set => SetProperty(ref _resultSummary, value);
    }

    public ICommand LoadResultsCommand { get; }
    public ICommand SaveScoresCommand { get; }
    public ICommand PublishResultsCommand { get; }

    private async Task LoadResultsAsync()
    {
        try
        {
            await LoadLookupDataAsync();
            await LoadResultEntriesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load results");
            MessageBox.Show($"Failed to load results: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadResultEntriesAsync()
    {
        _resultEntries.Clear();
        if (SelectedCourseForResults is null || string.IsNullOrWhiteSpace(ResultSemester)) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var enrollments = await context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == SelectedCourseForResults.Id && e.Semester == ResultSemester)
                .OrderBy(e => e.Student.LastName)
                .ToListAsync();

            foreach (var e in enrollments)
            {
                _resultEntries.Add(new ResultEntry
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student.FullName,
                    RegistrationNumber = e.Student.RegistrationNumber,
                    CaScore = e.CaScore,
                    ExamScore = e.ExamScore,
                    IsPublished = e.IsResultPublished,
                    ResultPublishedAt = e.ResultPublishedAt
                });
            }

            UpdateResultSummary();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load result entries");
        }
    }

    private async Task SaveScoresAsync()
    {
        if (_resultEntries.Count == 0) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var entry in _resultEntries)
            {
                if (!ResultComputationEngine.ValidateScores(entry.CaScore, entry.ExamScore, out var error))
                {
                    MessageBox.Show($"{entry.StudentName}: {error}", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var enrollment = await context.Enrollments.FindAsync(entry.EnrollmentId);
                if (enrollment is null) continue;

                if (enrollment.IsResultPublished)
                {
                    MessageBox.Show($"Results for {enrollment.Student.FullName} are already published and cannot be modified.",
                        "Published", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                enrollment.CaScore = entry.CaScore;
                enrollment.ExamScore = entry.ExamScore;
                enrollment.Grade = ResultComputationEngine.ComputeGrade(entry.CaScore, entry.ExamScore);
            }

            await context.SaveChangesAsync();
            await LoadResultEntriesAsync();
            NotificationCenter.Instance.Success("Scores saved and grades computed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save scores");
            MessageBox.Show($"Failed to save scores: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PublishResultsAsync()
    {
        if (_resultEntries.Count == 0) return;
        if (_resultEntries.Any(r => !r.CaScore.HasValue || !r.ExamScore.HasValue))
        {
            MessageBox.Show("All students must have scores before publishing results.",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Publish results for {_resultEntries.Count} students in {SelectedCourseForResults?.CourseName} ({ResultSemester})?",
            "Publish Results", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            foreach (var entry in _resultEntries)
            {
                var enrollment = await context.Enrollments.FindAsync(entry.EnrollmentId);
                if (enrollment is null || enrollment.IsResultPublished) continue;

                enrollment.IsResultPublished = true;
                enrollment.ResultPublishedAt = now;
                entry.IsPublished = true;
                entry.ResultPublishedAt = now;
            }

            await context.SaveChangesAsync();
            UpdateResultSummary();
            NotificationCenter.Instance.Success($"Results published for {SelectedCourseForResults?.CourseName}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to publish results");
            MessageBox.Show($"Failed to publish results: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateResultSummary()
    {
        var graded = _resultEntries.Where(r => r.Grade is not null).ToList();
        var published = _resultEntries.Count(r => r.IsPublished);
        var passes = graded.Count(r => ResultComputationEngine.IsPassingGrade(r.Grade));
        var fails = graded.Count - passes;
        var avgTotal = graded.Where(r => r.TotalScore.HasValue).Select(r => r.TotalScore!.Value).DefaultIfEmpty(0).Average();

        ResultSummary = $"Enrolled: {_resultEntries.Count}  |  Graded: {graded.Count}  |  Passed: {passes}  |  Failed: {fails}  |  Published: {published}  |  Avg Total: {avgTotal:F1}/100";
    }

    #endregion

    #region Payments Tab

    private readonly ObservableCollection<Payment> _payments = new();
    public ObservableCollection<Payment> Payments => _payments;

    private ObservableCollection<Student> _paymentStudents = new();
    public ObservableCollection<Student> PaymentStudents { get => _paymentStudents; set => SetProperty(ref _paymentStudents, value); }

    private Student? _selectedPaymentStudent;
    public Student? SelectedPaymentStudent
    {
        get => _selectedPaymentStudent;
        set => SetProperty(ref _selectedPaymentStudent, value);
    }

    private decimal _paymentAmount;
    public decimal PaymentAmount
    {
        get => _paymentAmount;
        set => SetProperty(ref _paymentAmount, value);
    }

    private string _paymentMethod = "Cash";
    public string PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    private string _paymentDescription = string.Empty;
    public string PaymentDescription
    {
        get => _paymentDescription;
        set => SetProperty(ref _paymentDescription, value);
    }

    private string _paymentSemester = string.Empty;
    public string PaymentSemester
    {
        get => _paymentSemester;
        set => SetProperty(ref _paymentSemester, value);
    }

    private decimal _totalPayments;
    public decimal TotalPayments
    {
        get => _totalPayments;
        set => SetProperty(ref _totalPayments, value);
    }

    private Payment? _selectedPayment;
    public Payment? SelectedPayment
    {
        get => _selectedPayment;
        set
        {
            if (SetProperty(ref _selectedPayment, value))
                ((AsyncRelayCommand)DeletePaymentCommand).RaiseCanExecuteChanged();
        }
    }

    public List<string> PaymentMethods { get; } = new() { "Cash", "Credit Card", "Bank Transfer", "Check", "Online" };
    public List<string> PaymentStatuses { get; } = new() { "Completed", "Pending", "Failed", "Refunded" };

    public ICommand LoadPaymentsCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand DeletePaymentCommand { get; }

    private async Task LoadPaymentsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var payments = await context.Payments
                .Include(p => p.Student)
                .OrderByDescending(p => p.PaymentDate)
                .Take(500)
                .ToListAsync();

            _payments.Clear();
            foreach (var p in payments) _payments.Add(p);

            TotalPayments = await context.Payments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount);

            await LoadLookupDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load payments");
            MessageBox.Show($"Failed to load payments: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AddPaymentAsync()
    {
        if (SelectedPaymentStudent is null)
        {
            MessageBox.Show("Please select a student.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (PaymentAmount <= 0)
        {
            MessageBox.Show("Amount must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Payments.Add(new Payment
            {
                StudentId = SelectedPaymentStudent.Id,
                Amount = PaymentAmount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = PaymentMethod,
                Description = string.IsNullOrWhiteSpace(PaymentDescription) ? null : PaymentDescription.Trim(),
                Status = "Completed",
                Semester = string.IsNullOrWhiteSpace(PaymentSemester) ? null : PaymentSemester.Trim()
            });

            await context.SaveChangesAsync();
            await LoadPaymentsAsync();
            SelectedPaymentStudent = null;
            PaymentAmount = 0;
            PaymentDescription = string.Empty;
            PaymentSemester = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add payment");
            MessageBox.Show($"Failed to add payment: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeletePaymentAsync()
    {
        if (SelectedPayment is null) return;
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var payment = await context.Payments.FindAsync(SelectedPayment.Id);
            if (payment is not null)
            {
                context.Payments.Remove(payment);
                await context.SaveChangesAsync();
            }
            await LoadPaymentsAsync();
            SelectedPayment = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete payment {PaymentId}", SelectedPayment?.Id);
            MessageBox.Show($"Failed to delete payment: {Unwrap(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadLookupDataAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var students = await context.Students.OrderBy(s => s.RegistrationNumber).ToListAsync();
        _studentsForEnroll.Clear();
        foreach (var s in students) _studentsForEnroll.Add(s);
        PaymentStudents = new ObservableCollection<Student>(students);

        var courses = await context.Courses.Include(c => c.PrerequisiteCourse).OrderBy(c => c.CourseCode).ToListAsync();
        _coursesForEnroll.Clear();
        foreach (var c in courses) _coursesForEnroll.Add(c);
        _scheduleCourses.Clear();
        foreach (var c in courses) _scheduleCourses.Add(c);

        var semesters = await context.Semesters.Where(s => s.IsActive).OrderByDescending(s => s.StartDate).Select(s => s.Name).ToListAsync();
        SemesterList = new ObservableCollection<string>(semesters);

        if (SemesterList.Count > 0)
        {
            if (!SemesterList.Contains(Semester))
                Semester = SemesterList[0];
            if (string.IsNullOrWhiteSpace(ResultSemester) || !SemesterList.Contains(ResultSemester))
                ResultSemester = SemesterList[0];
        }

        RemapSelections();
    }

    private void RemapSelections()
    {
        SelectedCourseForEnroll = _coursesForEnroll.FirstOrDefault(c => c.Id == SelectedCourseForEnroll?.Id);
        SelectedCourseForAttendance = _coursesForEnroll.FirstOrDefault(c => c.Id == SelectedCourseForAttendance?.Id);
        SelectedCourseForResults = _coursesForEnroll.FirstOrDefault(c => c.Id == SelectedCourseForResults?.Id);
        SelectedScheduleCourse = _scheduleCourses.FirstOrDefault(c => c.Id == SelectedScheduleCourse?.Id);
        SelectedStudentForEnroll = _studentsForEnroll.FirstOrDefault(s => s.Id == SelectedStudentForEnroll?.Id);
        SelectedPaymentStudent = PaymentStudents.FirstOrDefault(s => s.Id == SelectedPaymentStudent?.Id);
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

public sealed class AttendanceRecord
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Present";
    public string? Remarks { get; set; }
}

public sealed class AttendanceEntry
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Present";
    public string Remarks { get; set; } = string.Empty;
}

public sealed class ResultEntry : ViewModelBase
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;

    private decimal? _caScore;
    public decimal? CaScore
    {
        get => _caScore;
        set
        {
            if (SetProperty(ref _caScore, value))
            {
                OnPropertyChanged(nameof(TotalScore));
                OnPropertyChanged(nameof(Grade));
                OnPropertyChanged(nameof(GradePoint));
            }
        }
    }

    private decimal? _examScore;
    public decimal? ExamScore
    {
        get => _examScore;
        set
        {
            if (SetProperty(ref _examScore, value))
            {
                OnPropertyChanged(nameof(TotalScore));
                OnPropertyChanged(nameof(Grade));
                OnPropertyChanged(nameof(GradePoint));
            }
        }
    }

    public decimal? TotalScore => Services.ResultComputationEngine.ComputeTotalScore(CaScore, ExamScore);
    public string? Grade => Services.ResultComputationEngine.ComputeGrade(CaScore, ExamScore);
    public decimal? GradePoint => Services.ResultComputationEngine.GradePointFromGrade(Grade);

    private bool _isPublished;
    public bool IsPublished
    {
        get => _isPublished;
        set => SetProperty(ref _isPublished, value);
    }

    public DateTime? ResultPublishedAt { get; set; }
}

public sealed class DepartmentStat
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Percentage { get; set; }
}
