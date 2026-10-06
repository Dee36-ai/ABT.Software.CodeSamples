using MediatR;

namespace AppointmentBooking.Application;

public sealed record GetAvailableAppointmentSlotsQuery(
    Guid BusinessId,
    DateOnly Date)
    : IRequest<IReadOnlyList<AvailableAppointmentSlot>>;