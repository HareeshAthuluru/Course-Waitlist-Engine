# AI Prompt and Engineering Record

### Entry 1: Architecture & Concurrency Model
* **Engineering Activity**: Designing the state synchronization model for concurrent enrollment requests.
* **Prompt Used**:
  > "Design a high-throughput, thread-safe in-memory C# service for course enrollment with a waitlist. What locking mechanism ensures capacity is never exceeded without creating a global performance bottleneck?"
* **AI Output Summary**:
  > Suggested applying a `lock(this)` statement around the enrollment method or using a global `ReaderWriterLockSlim`.
* **Critical Review & Decision**:
  > **REJECTED**: A global `lock(this)` introduces global contention across unrelated courses. `ReaderWriterLockSlim` adds excessive overhead given that enrollment and waitlisting are write-heavy operations.
  > **ACCEPTED / MODIFIED**: Implemented a keyed `ConcurrentDictionary<string, SemaphoreSlim>` to enforce per-course mutual exclusion, allowing parallel execution across distinct course instances.
* **Verification Evidence**:
  > Verified via `ConcurrentEnrollment_NeverExceedsCapacity` executing 40 concurrent enrollment tasks against 5 seats with 0 capacity leaks.

---

### Entry 2: Waitlist Ineligibility Policy & FIFO Promotion
* **Engineering Activity**: Establishing behavior when a waitlisted learner becomes ineligible prior to seat availability.
* **Prompt Used**:
  > "In an automated FIFO course waitlist promotion system, what is the best software pattern if the person at the front of the queue no longer meets prerequisites when a seat opens up? Should they stay in the queue or be removed?"
* **AI Output Summary**:
  > Outlined two options: (A) Skip them and keep them at the top until they fulfill the prerequisite, or (B) Skip and purge them permanently from the waitlist.
* **Critical Review & Decision**:
  > **REJECTED Option A**: Retaining an ineligible learner at index 0 blocks the entire queue (Head-of-Line blocking), preventing eligible students behind them from ever getting promoted.
  > **ACCEPTED Option B**: Adopted the "Skip-and-Purge" model. Implemented a `while` loop that purges ineligible candidates and continues promoting until an eligible learner is found or the queue empties.
* **Verification Evidence**:
  > Verified via `Drop_AppliesSkipPolicy_WhenWaitlistedCandidateBecomesIneligible` test case.
