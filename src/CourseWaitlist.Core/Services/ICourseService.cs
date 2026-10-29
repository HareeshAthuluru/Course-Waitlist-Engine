using CourseWaitlist.Core.Domain;

namespace CourseWaitlist.Core.Services;

public interface ICourseService
{
    void AddCourse(CourseOffering course);
    Task<RegistrationResult> EnrollAsync(string courseId, Learner learner, CancellationToken ct = default);
    Task<RegistrationResult> DropAsync(string courseId, string learnerId, Func<string, Task<Learner?>> learnerResolver, CancellationToken ct = default);
    (int EnrolledCount, int WaitlistCount) GetCourseState(string courseId);
    IReadOnlyList<string> GetWaitlist(string courseId);
}
