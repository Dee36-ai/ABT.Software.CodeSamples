using AppointmentBooking.Application;
using AppointmentBooking.Domain;
using Moq;
using Xunit;

namespace AppointmentBooking.Tests;

public sealed class AvailabilityTests
{
    [Fact]
    public async Task Should_return_available_slots_for_matching_day()
    {
        var businessId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

        var scheduleRepository = new Mock<IAvailabilityScheduleRepository>();
        var slotRepository = new Mock<IAvailabilitySlotRepository>();
        var appointmentRepository = new Mock<IAppointmentRepository>();

        scheduleRepository
            .Setup(x => x.GetByBusinessAsync(
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailabilitySchedule(
                scheduleId,
                true,
                "Africa/Johannesburg"));

        slotRepository
            .Setup(x => x.GetByScheduleAsync(
                scheduleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AvailabilitySlot(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    60,
                    0,
                    0,
                    true)
            ]);

        appointmentRepository
            .Setup(x => x.GetByBusinessAndDateRangeAsync(
                businessId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetAvailableAppointmentSlotsQueryHandler(
            scheduleRepository.Object,
            slotRepository.Object,
            appointmentRepository.Object);

        var monday = new DateOnly(2026, 10, 5);

        var result = await handler.Handle(
            new GetAvailableAppointmentSlotsQuery(
                businessId,
                monday),
            CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(
            new DateTime(2026, 10, 5, 9, 0, 0),
            result[0].AppointmentDateTime);
    }

    [Fact]
    public async Task Should_exclude_slots_that_conflict_with_existing_appointments()
    {
        var businessId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

        var scheduleRepository = new Mock<IAvailabilityScheduleRepository>();
        var slotRepository = new Mock<IAvailabilitySlotRepository>();
        var appointmentRepository = new Mock<IAppointmentRepository>();

        scheduleRepository
            .Setup(x => x.GetByBusinessAsync(
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailabilitySchedule(
                scheduleId,
                true,
                "Africa/Johannesburg"));

        slotRepository
            .Setup(x => x.GetByScheduleAsync(
                scheduleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AvailabilitySlot(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    60,
                    0,
                    0,
                    true)
            ]);

        appointmentRepository
            .Setup(x => x.GetByBusinessAndDateRangeAsync(
                businessId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Appointment(
                    businessId,
                    "Existing Customer",
                    "0821234567",
                    null,
                    "Consultation",
                    new DateTime(
                        2026, 10, 5, 8, 0, 0,
                        DateTimeKind.Utc),
                    60,
                    null)
            ]);

        var handler = new GetAvailableAppointmentSlotsQueryHandler(
            scheduleRepository.Object,
            slotRepository.Object,
            appointmentRepository.Object);

        var result = await handler.Handle(
            new GetAvailableAppointmentSlotsQuery(
                businessId,
                new DateOnly(2026, 10, 5)),
            CancellationToken.None);

        Assert.DoesNotContain(
            result,
            x => x.AppointmentDateTime.Hour == 10);
    }

    [Fact]
    public async Task Should_return_empty_when_schedule_is_disabled()
    {
        var businessId = Guid.NewGuid();

        var scheduleRepository = new Mock<IAvailabilityScheduleRepository>();
        var slotRepository = new Mock<IAvailabilitySlotRepository>();
        var appointmentRepository = new Mock<IAppointmentRepository>();

        scheduleRepository
            .Setup(x => x.GetByBusinessAsync(
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailabilitySchedule(
                Guid.NewGuid(),
                false,
                "Africa/Johannesburg"));

        var handler = new GetAvailableAppointmentSlotsQueryHandler(
            scheduleRepository.Object,
            slotRepository.Object,
            appointmentRepository.Object);

        var result = await handler.Handle(
            new GetAvailableAppointmentSlotsQuery(
                businessId,
                new DateOnly(2026, 10, 5)),
            CancellationToken.None);

        Assert.Empty(result);
    }
}