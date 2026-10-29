using CourseWaitlist.Core.Domain;
using CourseWaitlist.Core.Services;
using Xunit;

namespace CourseWaitlist.Tests;

public class CourseServiceTests
{
    private readonly CourseService _service;
    private readonly Dictionary<string, Learner> _learnerDb;

    public CourseServiceTests()
    {
        _service = new CourseService();
        _learnerDb = new Dictionary<string, Learner>(StringComparer.OrdinalIgnoreCase);
    }

    private Task<Learner?> MockLearnerResolver(string learnerId)
    {
        _learnerDb.TryGetValue(learnerId, out var learner);
        return Task.FromResult(learner);
    }

    [Fact]
    public async Task Enroll_UnderCapacity_DirectlyEnrollsEligibleLearner()
    {
        var course = new CourseOffering("CS101", capacity: 2);
        _service.AddCourse(course);
        var learner = new Learner("L1");
        _learnerDb["L1"] = learner;

        var result = await _service.EnrollAsync("CS101", learner);

        Assert.True(result.Success);
        Assert.Equal(OperationResultCode.SuccessEnrolled, result.Code);
        var (enrolled, waitlist) = _service.GetCourseState("CS101");
        Assert.Equal(1, enrolled);
        Assert.Equal(0, waitlist);
    }

    [Fact]
    public async Task Enroll_Fails_WhenPrerequisiteNotSatisfied()
    {
        var course = new CourseOffering("CS201", capacity: 2, requiredPrerequisites: new[] { "CS101" });
        _service.AddCourse(course);
        var learner = new Learner("L1", completedPrerequisites: new[] { "MATH101" });

        var result = await _service.EnrollAsync("CS201", learner);

        Assert.False(result.Success);
        Assert.Equal(OperationResultCode.PrerequisiteNotMet, result.Code);
        var (enrolled, waitlist) = _service.GetCourseState("CS201");
        Assert.Equal(0, enrolled);
        Assert.Equal(0, waitlist);
    }

    [Fact]
    public async Task Enroll_FullCapacity_AddsEligibleLearnerToWaitlist_FIFO()
    {
        var course = new CourseOffering("CS101", capacity: 1);
        _service.AddCourse(course);
        var l1 = new Learner("L1");
        var l2 = new Learner("L2");

        await _service.EnrollAsync("CS101", l1);
        var r2 = await _service.EnrollAsync("CS101", l2);

        Assert.True(r2.Success);
        Assert.Equal(OperationResultCode.SuccessWaitlisted, r2.Code);
        var (enrolled, waitlist) = _service.GetCourseState("CS101");
        Assert.Equal(1, enrolled);
        Assert.Equal(1, waitlist);
        Assert.Equal("L2", _service.GetWaitlist("CS101")[0]);
    }

    [Fact]
    public async Task Enroll_RejectsDuplicate_EnrolledAndWaitlisted()
    {
        var course = new CourseOffering("CS101", capacity: 1);
        _service.AddCourse(course);
        var l1 = new Learner("L1");
        var l2 = new Learner("L2");

        await _service.EnrollAsync("CS101", l1);
        var dupEnroll = await _service.EnrollAsync("CS101", l1);

        Assert.False(dupEnroll.Success);
        Assert.Equal(OperationResultCode.AlreadyEnrolled, dupEnroll.Code);

        await _service.EnrollAsync("CS101", l2);
        var dupWaitlist = await _service.EnrollAsync("CS101", l2);

        Assert.False(dupWaitlist.Success);
        Assert.Equal(OperationResultCode.AlreadyWaitlisted, dupWaitlist.Code);

        var (enrolled, waitlist) = _service.GetCourseState("CS101");
        Assert.Equal(1, enrolled);
        Assert.Equal(1, waitlist);
    }

    [Fact]
    public async Task Drop_PromotesEarliestEligibleLearner_FIFO()
    {
        var course = new CourseOffering("CS101", capacity: 1);
        _service.AddCourse(course);
        var l1 = new Learner("L1");
        var l2 = new Learner("L2");
        _learnerDb["L1"] = l1;
        _learnerDb["L2"] = l2;

        await _service.EnrollAsync("CS101", l1);
        await _service.EnrollAsync("CS101", l2);

        var dropResult = await _service.DropAsync("CS101", "L1", MockLearnerResolver);

        Assert.True(dropResult.Success);
        Assert.Equal(OperationResultCode.SuccessDroppedWithPromotion, dropResult.Code);
        Assert.Equal("L2", dropResult.PromotedLearnerId);

        var (enrolled, waitlist) = _service.GetCourseState("CS101");
        Assert.Equal(1, enrolled);
        Assert.Equal(0, waitlist);
    }

    [Fact]
    public async Task Drop_AppliesSkipPolicy_WhenWaitlistedCandidateBecomesIneligible()
    {
        var course = new CourseOffering("CS201", capacity: 1, requiredPrerequisites: new[] { "CS101" });
        _service.AddCourse(course);

        var l1 = new Learner("L1", new[] { "CS101" });
        var l2Ineligible = new Learner("L2", Array.Empty<string>());
        var l3Eligible = new Learner("L3", new[] { "CS101" });

        _learnerDb["L1"] = l1;
        _learnerDb["L2"] = l2Ineligible;
        _learnerDb["L3"] = l3Eligible;

        await _service.EnrollAsync("CS201", l1);
        
        course.WaitlistLearnerIds.Add("L2");
        course.WaitlistLearnerIds.Add("L3");

        var dropResult = await _service.DropAsync("CS201", "L1", MockLearnerResolver);

        Assert.True(dropResult.Success);
        Assert.Equal(OperationResultCode.SuccessDroppedWithPromotion, dropResult.Code);
        Assert.Equal("L3", dropResult.PromotedLearnerId);
        Assert.Contains("L2", dropResult.SkippedLearnerIds!);

        var (enrolled, waitlist) = _service.GetCourseState("CS201");
        Assert.Equal(1, enrolled);
        Assert.Equal(0, waitlist);
    }

    [Fact]
    public async Task Drop_InvalidOrRepeated_ReturnsCleanResultWithoutStateCorruption()
    {
        var course = new CourseOffering("CS101", capacity: 2);
        _service.AddCourse(course);
        var l1 = new Learner("L1");
        _learnerDb["L1"] = l1;

        await _service.EnrollAsync("CS101", l1);

        var res1 = await _service.DropAsync("CS101", "L1", MockLearnerResolver);
        Assert.True(res1.Success);

        var res2 = await _service.DropAsync("CS101", "L1", MockLearnerResolver);
        Assert.False(res2.Success);
        Assert.Equal(OperationResultCode.NotEnrolled, res2.Code);

        var res3 = await _service.DropAsync("CS101", "UnknownLearner", MockLearnerResolver);
        Assert.False(res3.Success);
        Assert.Equal(OperationResultCode.NotEnrolled, res3.Code);

        var (enrolled, waitlist) = _service.GetCourseState("CS101");
        Assert.Equal(0, enrolled);
        Assert.Equal(0, waitlist);
    }

    [Fact]
    public async Task ConcurrentEnrollment_NeverExceedsCapacity()
    {
        const int capacity = 5;
        const int totalLearners = 40;
        var course = new CourseOffering("CONCUR_101", capacity: capacity);
        _service.AddCourse(course);

        var tasks = Enumerable.Range(1, totalLearners).Select(i =>
        {
            var learner = new Learner($"Learner_{i:D2}");
            return _service.EnrollAsync("CONCUR_101", learner);
        });

        await Task.WhenAll(tasks);

        var (enrolled, waitlist) = _service.GetCourseState("CONCUR_101");
        Assert.Equal(capacity, enrolled);
        Assert.Equal(totalLearners - capacity, waitlist);
    }
}
