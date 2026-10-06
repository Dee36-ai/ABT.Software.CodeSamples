using AppointmentBooking.Domain;
using MediatR;

namespace AppointmentBooking.Application;

public sealed class GetAvailableAppointmentSlotsQueryHandler
    : IRequestHandler<
        GetAvailableAppointmentSlotsQuery,
        IReadOnlyList<AvailableAppointmentSlot>>
{
    private readonly IAvailabilityScheduleRepository _scheduleRepository;
    private readonly IAvailabilitySlotRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public GetAvailableAppointmentSlotsQueryHandler(
        IAvailabilityScheduleRepository scheduleRepository,
        IAvailabilitySlotRepository slotRepository,
        IAppointmentRepository appointmentRepository)
    {
        _scheduleRepository = scheduleRepository;
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
    }

    public async Task<IReadOnlyList<AvailableAppointmentSlot>> Handle(
        GetAvailableAppointmentSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var schedule =
            await _scheduleRepository.GetByBusinessAsync(
                request.BusinessId,
                cancellationToken);

        if (schedule is null || !schedule.IsEnabled)
        {
            return Array.Empty<AvailableAppointmentSlot>();
        }

        var slots =
            await _slotRepository.GetByScheduleAsync(
                schedule.Id,
                cancellationToken);

        var matchingSlots = slots
            .Where(slot =>
                slot.DayOfWeek == request.Date.DayOfWeek &&
                slot.IsAvailable)
            .ToList();

        if (matchingSlots.Count == 0)
        {
            return Array.Empty<AvailableAppointmentSlot>();
        }

        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);

        var localDayStart = request.Date.ToDateTime(
            TimeOnly.MinValue,
            DateTimeKind.Unspecified);

        var localDayEnd = request.Date
            .AddDays(1)
            .ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Unspecified);

        var dayStartUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localDayStart,
                timeZone);

        var dayEndUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localDayEnd,
                timeZone);

        var appointments =
            await _appointmentRepository
                .GetByBusinessAndDateRangeAsync(
                    request.BusinessId,
                    dayStartUtc,
                    dayEndUtc,
                    cancellationToken);

        var results = new List<AvailableAppointmentSlot>();

        foreach (var slot in matchingSlots)
        {
            var localStart = request.Date.ToDateTime(
                slot.StartTime,
                DateTimeKind.Unspecified);

            var localEnd = request.Date.ToDateTime(
                slot.EndTime,
                DateTimeKind.Unspecified);

            var stepMinutes =
                slot.AppointmentDurationMinutes +
                slot.BufferBeforeMinutes +
                slot.BufferAfterMinutes;

            for (
                var current = localStart;
                current.AddMinutes(
                    slot.AppointmentDurationMinutes +
                    slot.BufferAfterMinutes) <= localEnd;
                current = current.AddMinutes(stepMinutes))
            {
                var appointmentStart =
                    current.AddMinutes(
                        slot.BufferBeforeMinutes);

                var appointmentEnd =
                    appointmentStart.AddMinutes(
                        slot.AppointmentDurationMinutes);

                var appointmentStartUtc =
                    TimeZoneInfo.ConvertTimeToUtc(
                        appointmentStart,
                        timeZone);

                var appointmentEndUtc =
                    TimeZoneInfo.ConvertTimeToUtc(
                        appointmentEnd,
                        timeZone);

                var hasConflict =
                    appointments.Any(existingAppointment =>
                        existingAppointment.AppointmentDateUtc <
                            appointmentEndUtc &&
                        existingAppointment.AppointmentDateUtc
                            .AddMinutes(existingAppointment.DurationMinutes) >
                            appointmentStartUtc);

                if (hasConflict)
                {
                    continue;
                }

                results.Add(
                    new AvailableAppointmentSlot(
                        appointmentStart,
                        slot.AppointmentDurationMinutes));
            }
        }

        return results
            .OrderBy(x => x.AppointmentDateTime)
            .ToList();
    }
}