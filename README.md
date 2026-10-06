# Appointment Booking — Code Sample

A selected and anonymised code sample demonstrating how I approach
business workflows in a .NET SaaS application.

## Engineering concepts

- Clean Architecture
- CQRS with MediatR
- Domain-driven design
- Repository abstraction
- Domain encapsulation
- Time-zone-aware scheduling
- Appointment conflict detection
- Business rule validation
- Unit testing with xUnit and Moq
- Cancellation token propagation

## Workflow

The sample demonstrates an appointment booking workflow that:

1. Loads business availability rules.
2. Resolves availability in the business time zone.
3. Calculates valid appointment slots.
4. Detects conflicts with existing appointments.
5. Re-validates the requested slot before booking.
6. Creates the domain entity.
7. Persists the appointment.

## Engineering decisions

### Domain-driven design

The `Appointment` entity encapsulates its own state and business rules.

Consumers cannot directly modify appointment state through public
setters. State changes are performed through domain behaviour such as
`Reschedule`, `Complete`, and `Cancel`.

### CQRS

Commands and queries are separated using MediatR.

- `CreateAppointmentCommand` handles state-changing operations.
- `GetAvailableAppointmentSlotsQuery` handles read operations.

This keeps read and write responsibilities separate and makes the
application workflow easier to reason about.

### Time-zone handling

Appointment timestamps are persisted in UTC, while availability is
evaluated in the business's configured time zone.

This prevents the scheduling logic from depending on the server's
local time and supports businesses operating across different time
zones.

### Availability and conflict detection

Available slots are calculated from configured working hours,
appointment duration, and buffer periods.

Existing appointments are checked for overlapping time ranges before
a slot is returned as available.

The requested slot is then re-validated during the booking workflow
before the appointment is persisted.

### Separation of concerns

The application layer coordinates the booking workflow without
depending directly on database implementations.

Repository interfaces define the persistence requirements while keeping
infrastructure concerns outside the application layer.

### Reliability considerations

An availability check is not a guarantee that a slot will remain
available.

In a production deployment, the final persistence boundary should also
enforce concurrency protection so simultaneous requests cannot create
conflicting appointments.

## Testing

The sample includes automated tests covering:

- Appointment creation
- Invalid appointment duration
- Appointment cancellation
- Appointment rescheduling
- Disabled availability
- Available appointment slots
- Conflicting appointments
- Time-zone-aware booking
- Rejection of a slot that is no longer available

Run the test suite with:

```bash
dotnet test