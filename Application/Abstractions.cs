using AppointmentBooking.Domain;

namespace AppointmentBooking.Application;

public sealed record AvailableAppointmentSlot(
    DateTime AppointmentDateTime,
    int DurationMinutes);

public sealed record AvailabilitySchedule(
    Guid Id,
    bool IsEnabled,
    string TimeZone);

public sealed record AvailabilitySlot(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int AppointmentDurationMinutes,
    int BufferBeforeMinutes,
    int BufferAfterMinutes,
    bool IsAvailable);

public interface IAvailabilityScheduleRepository
{
    Task<AvailabilitySchedule?> GetByBusinessAsync(
        Guid businessId,
        CancellationToken cancellationToken);
}

public interface IAvailabilitySlotRepository
{
    Task<IReadOnlyList<AvailabilitySlot>> GetByScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken);
}

public interface IAppointmentRepository
{
    Task<IReadOnlyList<Appointment>> GetByBusinessAndDateRangeAsync(
        Guid businessId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken);

    Task AddAsync(
        Appointment appointment,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}