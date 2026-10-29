# Limitations and Follow-Up Risks

1. **In-Memory Volatility**: Course and waitlist states reside in volatile memory. A crash or container restart will discard current enrollments and waitlist positions.
   * *Mitigation*: Persist state to a relational store using an atomic transaction with optimistic concurrency tokens (`rowversion` / `xmin`).
2. **Synchronous Queue Drainage**: Resolving learner profiles during `DropAsync` introduces synchronous latency into the drop path. If many consecutive candidates fail prerequisites, the lock duration increases.
   * *Mitigation*: Shift to an event-driven model: `DropAsync` publishes a `SeatVacatedEvent` to an event stream (e.g., Kafka); a background worker asynchronously consumes the event, resolves the next eligible learner, and notifies them.
3. **Waitlist Expiration & Claims**: Waitlisted candidates are promoted immediately without manual acceptance.
   * *Mitigation*: Implement a reservation window (e.g., 24-hour claim window) allowing learners to confirm enrollment before moving to the next person.
