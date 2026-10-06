namespace AppointmentBooking.Domain;

public sealed class Appointment
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BusinessId { get; private set; }

    public string CustomerName { get; private set; }

    public string CustomerPhone { get; private set; }

    public string? CustomerEmail { get; private set; }

    public string Service { get; private set; }

    public DateTime AppointmentDateUtc { get; private set; }

    public int DurationMinutes { get; private set; }

    public string? Notes { get; private set; }

    public AppointmentStatus Status { get; private set; }

    private Appointment()
    {
        CustomerName = null!;
        CustomerPhone = null!;
        Service = null!;
    }

    public Appointment(
        Guid businessId,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string service,
        DateTime appointmentDateUtc,
        int durationMinutes,
        string? notes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        BusinessId = businessId;

        CustomerName = ValidateRequired(customerName, nameof(customerName));
        CustomerPhone = ValidateRequired(customerPhone, nameof(customerPhone));
        CustomerEmail = customerEmail?.Trim();
        Service = ValidateRequired(service, nameof(service));
        Notes = notes?.Trim();

        AppointmentDateUtc = appointmentDateUtc;
        DurationMinutes = durationMinutes;
        Status = AppointmentStatus.Scheduled;
    }

    public void Reschedule(
        DateTime appointmentDateUtc,
        int durationMinutes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        AppointmentDateUtc = appointmentDateUtc;
        DurationMinutes = durationMinutes;
    }

    public void Complete() =>
        Status = AppointmentStatus.Completed;

    public void Cancel() =>
        Status = AppointmentStatus.Cancelled;

    private static string ValidateRequired(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        return value.Trim();
    }
}