# Course Registration Rubric Self-Scorecard

| Evaluation Dimension | Assessment Target | Self-Rating | Evidence & File Location |
| :--- | :--- | :--- | :--- |
| **Problem Decomposition** | Bounded slice, clear separation of domain logic, state models, and concurrency | Exceeds | `CourseService.cs`, `Models.cs`, `DESIGN.md` |
| **Engineering Invariants** | Capacity upper bounds, duplicate enrollment prevention, idempotent cancellations | Exceeds | Unit tests in `CourseServiceTests.cs` |
| **Waitlist & Policy Consistency** | Explicit, documented skip-and-purge policy applied consistently across lifecycle | Exceeds | `DropAsync` method & `Drop_AppliesSkipPolicy...` test |
| **Concurrency Safety** | Thread-safe state mutation under parallel load without race conditions | Exceeds | `ConcurrentDictionary<string, SemaphoreSlim>` & `ConcurrentEnrollment_NeverExceedsCapacity` |
| **AI-Assisted Engineering** | Explicit record of prompt strategy, critical evaluation, and documented edits/rejections | Exceeds | Detailed review entries in `AI_USAGE_LOG.md` |
| **Deliverable Quality** | Complete build/run instructions, console demo prototype, follow-up trade-offs | Meets | `Program.cs`, `README.md`, `LIMITATIONS.md` |
