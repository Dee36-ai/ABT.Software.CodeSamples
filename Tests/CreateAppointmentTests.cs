using AppointmentBooking.Application;
using AppointmentBooking.Domain;
using MediatR;
using Moq;
using Xunit;

namespace AppointmentBooking.Tests;

public sealed class CreateAppointmentTests
{
    [Fact]
    public async Task Should_create_appointment_when_requested_slot_is_available()
    {
        var businessId = Guid.NewGuid();

        var appointmentRepository = new Mock<IAppointmentRepository>();
        var scheduleRepository = new Mock<IAvailabilityScheduleRepository>();
        var sender = new Mock<ISender>();
        var unitOfWork = new Mock<IUnitOfWork>();

        scheduleRepository
            .Setup(x => x.GetByBusinessAsync(
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailabilitySchedule(
                Guid.NewGuid(),
                true,
                "Africa/Johannesburg"));

        var appointmentDateUtc = new DateTime(
            2026, 10, 5, 7, 0, 0,
            DateTimeKind.Utc);

        var appointmentDateLocal = new DateTime(
            2026, 10, 5, 9, 0, 0);

        sender
            .Setup(x => x.Send(
                It.IsAny<GetAvailableAppointmentSlotsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AvailableAppointmentSlot(
                    appointmentDateLocal,
                    60)
            ]);

        Appointment? savedAppointment = null;

        appointmentRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Appointment>(),
                It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>(
                (appointment, _) => savedAppointment = appointment)
            .Returns(Task.CompletedTask);

        var handler = new CreateAppointmentCommandHandler(
            appointmentRepository.Object,
            scheduleRepository.Object,
            sender.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateAppointmentCommand(
                businessId,
                "Jane Smith",
                "0821234567",
                "jane@example.com",
                "Consultation",
                appointmentDateUtc,
                60,
                null),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(savedAppointment);
        Assert.Equal(
            "Jane Smith",
            savedAppointment!.CustomerName);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_reject_booking_when_slot_is_no_longer_available()
    {
        var businessId = Guid.NewGuid();

        var appointmentRepository = new Mock<IAppointmentRepository>();
        var scheduleRepository = new Mock<IAvailabilityScheduleRepository>();
        var sender = new Mock<ISender>();
        var unitOfWork = new Mock<IUnitOfWork>();

        scheduleRepository
            .Setup(x => x.GetByBusinessAsync(
                businessId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailabilitySchedule(
                Guid.NewGuid(),
                true,
                "Africa/Johannesburg"));

        sender
            .Setup(x => x.Send(
                It.IsAny<GetAvailableAppointmentSlotsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new CreateAppointmentCommandHandler(
            appointmentRepository.Object,
            scheduleRepository.Object,
            sender.Object,
            unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreateAppointmentCommand(
                    businessId,
                    "Jane Smith",
                    "0821234567",
                    null,
                    "Consultation",
                    new DateTime(
                        2026, 10, 5, 9, 0, 0,
                        DateTimeKind.Utc),
                    60,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "The requested appointment time is no longer available.",
            exception.Message);

        appointmentRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Appointment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}