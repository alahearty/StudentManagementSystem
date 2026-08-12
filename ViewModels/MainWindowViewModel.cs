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

        LoadAttendanceCommand = new AsyncRelayCommand(async _ => await LoadAttendanceAsync());
        SaveAttendanceCommand = new AsyncRelayCommand(async _ => await SaveAttendanceAsync());

        LoadSchedulesCommand = new AsyncRelayCommand(async _ => await LoadSchedulesAsync());
        AddScheduleCommand = new AsyncRelayCommand(async _ => await AddScheduleAsync());
        DeleteScheduleCommand = new AsyncRelayCommand(async _ => await DeleteScheduleAsync(), _ => SelectedSchedule is not null);

        LoadDashboardCommand = new AsyncRelayCommand(async _ => await LoadDashboardAsync());
        ExportStudentsCommand = new AsyncRelayCommand(async _ => await ExportStudentsAsync());
        ExportEnrollmentsCommand = new AsyncRelayCommand(async _ => await ExportEnrollmentsAsync());

        LoadPaymentsCommand = new AsyncRelayCommand(async _ => await LoadPaymentsAsync());
        AddPaymentCommand = new AsyncRelayCommand(async _ => await AddPaymentAsync());
        DeletePaymentCommand = new AsyncRelayCommand(async _ => await DeletePaymentAsync(), _ => SelectedPayment is not null);
    }

    public bool IsAdmin { get; }
    public string CurrentUserDisplayName { get; }
    public string CurrentUserRole { get; }
    public ICommand LogoutCommand { get; }

    private void Logout()
    {
        App.CurrentUser = null;
        var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is not LoginWindow)?.Close();
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
        var window = new TranscriptWindow(transcriptVm) { Owner = Application.Current.MainWindow };
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

            var courses = await context.Courses.Include(c => c.PrerequisiteCourse).OrderBy(c => c.CourseCode).ToListAsync();
            _coursesForEnroll.Clear();
            foreach (var c in courses) _coursesForEnroll.Add(c);
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

        if (!string.IsNullOrWhiteSpace(SelectedEnrollment.Grade) && !ValidationHelper.IsValidGrade(SelectedEnrollment.Grade))
        {
            MessageBox.Show("Invalid grade. Use A+, A, A-, B+, B, B-, C+, C, C-, D+, D, D-, or F.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

            var courses = await context.Courses.OrderBy(c => c.CourseCode).ToListAsync();
            _coursesForEnroll.Clear();
            foreach (var c in courses) _coursesForEnroll.Add(c);
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

            var courses = await context.Courses.OrderBy(c => c.CourseCode).ToListAsync();
            _scheduleCourses.Clear();
            foreach (var c in courses) _scheduleCourses.Add(c);

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

            var graded = await context.Enrollments
                .Where(e => e.Grade != null && e.Grade != "F")
                .ToListAsync();
            if (graded.Count > 0)
            {
                double totalGp = 0;
                foreach (var g in graded)
                {
                    totalGp += GradeCalculator.GetGradePoint(g.Grade);
                }
                DashboardAvgGpa = Math.Round(totalGp / graded.Count, 2);
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

            var students = await context.Students.OrderBy(s => s.RegistrationNumber).ToListAsync();
            PaymentStudents = new ObservableCollection<Student>(students);
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
