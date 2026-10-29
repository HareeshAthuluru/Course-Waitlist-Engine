using System.Collections.Concurrent;
using CourseWaitlist.Core.Domain;

namespace CourseWaitlist.Core.Services;

public class CourseService : ICourseService
{
    private readonly ConcurrentDictionary<string, CourseOffering> _courses = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _courseLocks = new(StringComparer.OrdinalIgnoreCase);

    public void AddCourse(CourseOffering course)
    {
        ArgumentNullException.ThrowIfNull(course);
        _courses[course.CourseId] = course;
    }

    private SemaphoreSlim GetLock(string courseId) =>
        _courseLocks.GetOrAdd(courseId, _ => new SemaphoreSlim(1, 1));

    public async Task<RegistrationResult> EnrollAsync(string courseId, Learner learner, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(learner);
        if (string.IsNullOrWhiteSpace(courseId))
            return new RegistrationResult(false, OperationResultCode.InvalidOperation, "Course ID cannot be empty.", learner.LearnerId);

        if (!_courses.TryGetValue(courseId, out var course))
            return new RegistrationResult(false, OperationResultCode.CourseNotFound, $"Course '{courseId}' was not found.", learner.LearnerId);

        var gate = GetLock(courseId);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (course.EnrolledLearnerIds.Contains(learner.LearnerId))
            {
                return new RegistrationResult(false, OperationResultCode.AlreadyEnrolled, 
                    $"Learner '{learner.LearnerId}' is already enrolled in '{courseId}'.", learner.LearnerId);
            }

            if (course.WaitlistLearnerIds.Contains(learner.LearnerId, StringComparer.OrdinalIgnoreCase))
            {
                return new RegistrationResult(false, OperationResultCode.AlreadyWaitlisted, 
                    $"Learner '{learner.LearnerId}' is already in the waitlist for '{courseId}'.", learner.LearnerId);
            }

            if (!MeetsPrerequisites(course, learner))
            {
                return new RegistrationResult(false, OperationResultCode.PrerequisiteNotMet, 
                    $"Learner '{learner.LearnerId}' does not meet required prerequisites for '{courseId}'.", learner.LearnerId);
            }

            if (course.EnrolledLearnerIds.Count < course.Capacity)
            {
                course.EnrolledLearnerIds.Add(learner.LearnerId);
                return new RegistrationResult(true, OperationResultCode.SuccessEnrolled, 
                    $"Learner '{learner.LearnerId}' successfully enrolled in '{courseId}'.", learner.LearnerId);
            }

            course.WaitlistLearnerIds.Add(learner.LearnerId);
            return new RegistrationResult(true, OperationResultCode.SuccessWaitlisted, 
                $"Course '{courseId}' is at capacity ({course.Capacity}). Learner '{learner.LearnerId}' joined waitlist at position {course.WaitlistLearnerIds.Count}.", learner.LearnerId);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<RegistrationResult> DropAsync(
        string courseId, 
        string learnerId, 
        Func<string, Task<Learner?>> learnerResolver, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(learnerResolver);
        if (string.IsNullOrWhiteSpace(learnerId))
            return new RegistrationResult(false, OperationResultCode.InvalidOperation, "Learner ID cannot be empty.", learnerId);

        if (string.IsNullOrWhiteSpace(courseId) || !_courses.TryGetValue(courseId, out var course))
            return new RegistrationResult(false, OperationResultCode.CourseNotFound, $"Course '{courseId}' was not found.", learnerId);

        var gate = GetLock(courseId);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var trimmedLearnerId = learnerId.Trim();

            if (!course.EnrolledLearnerIds.Remove(trimmedLearnerId))
            {
                return new RegistrationResult(false, OperationResultCode.NotEnrolled, 
                    $"Learner '{trimmedLearnerId}' is not currently enrolled in course '{courseId}'.", trimmedLearnerId);
            }

            string? promotedLearnerId = null;
            var skippedLearners = new List<string>();

            while (course.WaitlistLearnerIds.Count > 0 && course.EnrolledLearnerIds.Count < course.Capacity)
            {
                var candidateId = course.WaitlistLearnerIds[0];
                course.WaitlistLearnerIds.RemoveAt(0);

                var candidate = await learnerResolver(candidateId).ConfigureAwait(false);

                if (candidate != null && MeetsPrerequisites(course, candidate))
                {
                    course.EnrolledLearnerIds.Add(candidate.LearnerId);
                    promotedLearnerId = candidate.LearnerId;
                    break;
                }
                else
                {
                    skippedLearners.Add(candidateId);
                }
            }

            if (promotedLearnerId != null)
            {
                return new RegistrationResult(
                    true, 
                    OperationResultCode.SuccessDroppedWithPromotion, 
                    $"Learner '{trimmedLearnerId}' dropped. Candidate '{promotedLearnerId}' was promoted from the waitlist.", 
                    trimmedLearnerId, 
                    promotedLearnerId, 
                    skippedLearners);
            }

            return new RegistrationResult(
                true, 
                OperationResultCode.SuccessDroppedSeatVacant, 
                $"Learner '{trimmedLearnerId}' dropped. Seat is currently open (no eligible waitlist candidates).", 
                trimmedLearnerId, 
                null, 
                skippedLearners);
        }
        finally
        {
            gate.Release();
        }
    }

    public (int EnrolledCount, int WaitlistCount) GetCourseState(string courseId)
    {
        if (!_courses.TryGetValue(courseId, out var course))
            throw new KeyNotFoundException($"Course '{courseId}' was not found.");

        return (course.EnrolledLearnerIds.Count, course.WaitlistLearnerIds.Count);
    }

    public IReadOnlyList<string> GetWaitlist(string courseId)
    {
        if (!_courses.TryGetValue(courseId, out var course))
            throw new KeyNotFoundException($"Course '{courseId}' was not found.");

        return course.WaitlistLearnerIds.AsReadOnly();
    }

    private static bool MeetsPrerequisites(CourseOffering course, Learner learner)
    {
        if (course.RequiredPrerequisites.Count == 0) return true;
        return course.RequiredPrerequisites.IsSubsetOf(learner.CompletedPrerequisites);
    }
}
