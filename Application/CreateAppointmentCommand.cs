using MediatR;

namespace AppointmentBooking.Application;

public sealed record CreateAppointmentCommand(
    Guid BusinessId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string Service,
    DateTime AppointmentDateUtc,
    int DurationMinutes,
    string? Notes) : IRequest<Guid>;