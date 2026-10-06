using AppointmentBooking.Domain;
using MediatR;

namespace AppointmentBooking.Application;

public sealed class CreateAppointmentCommandHandler
    : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityScheduleRepository _scheduleRepository;
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IAvailabilityScheduleRepository scheduleRepository,
        ISender sender,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _scheduleRepository = scheduleRepository;
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var schedule =
            await _scheduleRepository.GetByBusinessAsync(
                request.BusinessId,
                cancellationToken);

        if (schedule is null || !schedule.IsEnabled)
        {
            throw new InvalidOperationException(
                "Appointment availability is not configured.");
        }

        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                schedule.TimeZone);

        var appointmentDateUtc =
            request.AppointmentDateUtc.Kind == DateTimeKind.Utc
                ? request.AppointmentDateUtc
                : DateTime.SpecifyKind(
                    request.AppointmentDateUtc,
                    DateTimeKind.Utc);

        var appointmentDateLocal =
            TimeZoneInfo.ConvertTimeFromUtc(
                appointmentDateUtc,
                timeZone);

        var availableSlots =
            await _sender.Send(
                new GetAvailableAppointmentSlotsQuery(
                    request.BusinessId,
                    DateOnly.FromDateTime(
                        appointmentDateLocal)),
                cancellationToken);

        var requestedSlotIsAvailable =
            availableSlots.Any(slot =>
                slot.AppointmentDateTime == appointmentDateLocal &&
                slot.DurationMinutes == request.DurationMinutes);

        if (!requestedSlotIsAvailable)
        {
            throw new InvalidOperationException(
                "The requested appointment time is no longer available.");
        }

        var appointment = new Appointment(
            request.BusinessId,
            request.CustomerName,
            request.CustomerPhone,
            request.CustomerEmail,
            request.Service,
            appointmentDateUtc,
            request.DurationMinutes,
            request.Notes);

        await _appointmentRepository.AddAsync(
            appointment,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return appointment.Id;
    }
}