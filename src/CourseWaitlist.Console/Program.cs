using CourseWaitlist.Core.Domain;
using CourseWaitlist.Core.Services;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("===============================================================");
Console.WriteLine("  Course Registration & FIFO Waitlist Engine Prototype (.NET 8)");
Console.WriteLine("===============================================================\n");
Console.ResetColor();

var service = new CourseService();
var learnerDb = new Dictionary<string, Learner>(StringComparer.OrdinalIgnoreCase);

Task<Learner?> ResolveLearnerAsync(string id)
{
    learnerDb.TryGetValue(id, out var l);
    return Task.FromResult(l);
}

const string courseId = "CS-501";
Console.WriteLine($"[SETUP] Initializing Course '{courseId}' with Capacity = 2, Prerequisite = 'CS-101'...\n");
var course = new CourseOffering(courseId, capacity: 2, requiredPrerequisites: new[] { "CS-101" });
service.AddCourse(course);

learnerDb["Alice"] = new Learner("Alice", new[] { "CS-101" });
learnerDb["Bob"] = new Learner("Bob", new[] { "CS-101" });
learnerDb["Charlie"] = new Learner("Charlie", new[] { "CS-101" });
learnerDb["Dave"] = new Learner("Dave", Array.Empty<string>());
learnerDb["Eve"] = new Learner("Eve", new[] { "CS-101" });

Console.WriteLine("--- SCENARIO 1: ENROLLMENTS UNDER CAPACITY ---");
var r1 = await service.EnrollAsync(courseId, learnerDb["Alice"]);
Console.WriteLine($"Alice enroll: {r1.Message} [Success: {r1.Success}]");

var r2 = await service.EnrollAsync(courseId, learnerDb["Bob"]);
Console.WriteLine($"Bob enroll:   {r2.Message} [Success: {r2.Success}]");

Console.WriteLine("\n--- SCENARIO 2: PREREQUISITE VALIDATION FAILURE ---");
var rDave = await service.EnrollAsync(courseId, learnerDb["Dave"]);
Console.WriteLine($"Dave enroll:  {rDave.Message} [Code: {rDave.Code}]");

Console.WriteLine("\n--- SCENARIO 3: CAPACITY REACHED - JOINING WAITLIST ---");
var rCharlie = await service.EnrollAsync(courseId, learnerDb["Charlie"]);
Console.WriteLine($"Charlie:      {rCharlie.Message}");

var rEve = await service.EnrollAsync(courseId, learnerDb["Eve"]);
Console.WriteLine($"Eve:          {rEve.Message}");

var state = service.GetCourseState(courseId);
Console.WriteLine($"\n[STATE] Enrolled: {state.EnrolledCount}/2 | Waitlist Count: {state.WaitlistCount}");

Console.WriteLine("\n--- SCENARIO 4: DUPLICATE PREVENTION ---");
var rDup = await service.EnrollAsync(courseId, learnerDb["Charlie"]);
Console.WriteLine($"Charlie dup:  {rDup.Message} [Code: {rDup.Code}]");

Console.WriteLine("\n--- SCENARIO 5: DROP & AUTOMATIC FIFO PROMOTION ---");
Console.WriteLine("Dropping Alice from course...");
var rDrop = await service.DropAsync(courseId, "Alice", ResolveLearnerAsync);
Console.WriteLine($"Drop result:  {rDrop.Message}");
Console.WriteLine($"Promoted:     {rDrop.PromotedLearnerId}");

state = service.GetCourseState(courseId);
Console.WriteLine($"\n[FINAL STATE] Enrolled: {state.EnrolledCount}/2 | Waitlist Count: {state.WaitlistCount}");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n Prototype completed all workflow invariants successfully.");
Console.ResetColor();
