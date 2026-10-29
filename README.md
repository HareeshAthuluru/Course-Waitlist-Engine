# Course Waitlist Engine (.NET 8)

A bounded, thread-safe course registration and waitlist promotion engine built with .NET 8, Clean Architecture principles, and spec-driven engineering.

## Key Features
- **Deterministic Capacity Invariants**: Guarantees capacity cannot be exceeded under concurrent load.
- **FIFO Waitlist Promotion**: Automatically promotes the earliest eligible waitlisted learner upon seat vacancy.
- **Skip-and-Purge Policy**: Avoids Head-of-Line blocking by skipping and removing ineligible candidates during promotion.
- **Per-Course Concurrency Isolation**: Uses fine-grained async locking via `SemaphoreSlim`.

---

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Build and Run

### 1. Run the Interactive Console Demo
```bash
dotnet run --project src/CourseWaitlist.Console
```

### 2. Run the Automated Test Suite
```bash
dotnet test --verbosity normal
```

---

## Documentation Links
- **Architectural Specification & Invariants**: [DESIGN.md](DESIGN.md)
- **AI Prompt & Engineering Log**: [AI_USAGE_LOG.md](AI_USAGE_LOG.md)
- **Trade-Offs & Future Risks**: [LIMITATIONS.md](LIMITATIONS.md)
- **Rubric Self-Scorecard**: [Course-Registration-Rubric-Score.md](Course-Registration-Rubric-Score.md)
