namespace CourseWaitlist.Core.Domain;

public enum EnrollmentStatus
{
    Enrolled,
    Waitlisted,
    Dropped,
    Rejected
}

public enum OperationResultCode
{
    SuccessEnrolled,
    SuccessWaitlisted,
    SuccessDroppedWithPromotion,
    SuccessDroppedSeatVacant,
    AlreadyEnrolled,
    AlreadyWaitlisted,
    PrerequisiteNotMet,
    NotEnrolled,
    CourseNotFound,
    InvalidOperation
}

public record RegistrationResult(
    bool Success,
    OperationResultCode Code,
    string Message,
    string? LearnerId,
    string? PromotedLearnerId = null,
    IReadOnlyList<string>? SkippedLearnerIds = null
);

public class Learner
{
    public string LearnerId { get; }
    public HashSet<string> CompletedPrerequisites { get; }

    public Learner(string learnerId, IEnumerable<string>? completedPrerequisites = null)
    {
        if (string.IsNullOrWhiteSpace(learnerId))
            throw new ArgumentException("Learner ID cannot be null or empty.", nameof(learnerId));

        LearnerId = learnerId.Trim();
        CompletedPrerequisites = completedPrerequisites != null 
            ? new HashSet<string>(completedPrerequisites, StringComparer.OrdinalIgnoreCase) 
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

public class CourseOffering
{
    public string CourseId { get; }
    public int Capacity { get; }
    public IReadOnlySet<string> RequiredPrerequisites { get; }

    public HashSet<string> EnrolledLearnerIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> WaitlistLearnerIds { get; } = new();

    public CourseOffering(string courseId, int capacity, IEnumerable<string>? requiredPrerequisites = null)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            throw new ArgumentException("Course ID cannot be null or empty.", nameof(courseId));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be strictly greater than 0.");

        CourseId = courseId.Trim();
        Capacity = capacity;
        RequiredPrerequisites = requiredPrerequisites != null 
            ? new HashSet<string>(requiredPrerequisites, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
