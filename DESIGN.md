# Design Specification: Bounded Course Waitlist Service

## 1. Domain Boundaries & Scope
The service implements an isolated vertical slice for single-course registration, cancellation, and automated FIFO waitlist promotion.
- **In-Scope**: Capacity enforcement, prerequisite evaluation, duplicate rejection, deterministic promotion, and concurrency synchronization.
- **Out-of-Scope**: Payment gateways, multi-course degree audits, notification dispatchers, identity provisioning, and UI layers.

## 2. Core Invariants
1. **Capacity Upper Bound**: At any point in time, `EnrolledLearnerIds.Count <= Capacity`.
2. **Mutual Exclusivity**: A learner ID cannot exist simultaneously across both `EnrolledLearnerIds` and `WaitlistLearnerIds`.
3. **Queue Fairness**: Waitlisted candidates are queued strictly in order of receipt (FIFO).
4. **Idempotent Operations**: Dropping an unregistered learner or repeatedly dropping an enrolled learner must yield a deterministic failure (`OperationResultCode.NotEnrolled`) without modifying internal collections.

## 3. Prerequisite & Waitlist Promotion Policy
- **At Enrollment**: Prerequisite satisfaction is a prerequisite to either direct enrollment or joining the waitlist. Ineligible learners are rejected immediately (`PrerequisiteNotMet`).
- **At Waitlist Promotion (Skip-and-Purge)**:
  - If a seat becomes vacant via `DropAsync`, the engine inspects the waitlist in FIFO order.
  - If the candidate at the head of the waitlist fails prerequisite re-validation or cannot be resolved from the profile store, they are **skipped and permanently purged** from the waitlist.
  - **Tradeoff Rationale**: Retaining an ineligible learner at the head of the queue would result in Head-of-Line (HoL) blocking, preventing qualified learners further back from claiming open seats. Purging the record preserves queue velocity and fairness.

## 4. Concurrency & Isolation Pattern
- Thread safety is enforced per course using a `ConcurrentDictionary<string, SemaphoreSlim>`.
- **Granular Isolation**: Locks are acquired strictly at the course boundary. Concurrent enrollments across different courses operate independently without thread contention.
- **Atomic Operations**: Verification of remaining capacity, duplicate checks, mutation of state sets, and waitlist drainage execute under mutual exclusion.
