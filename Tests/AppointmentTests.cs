using AppointmentBooking.Domain;
using Xunit;

namespace AppointmentBooking.Tests;

public sealed class AppointmentTests
{
	[Fact]
	public void Should_create_appointment_with_scheduled_status()
	{
		var appointment = new Appointment(
			Guid.NewGuid(),
			"Jane Smith",
			"0821234567",
			"jane@example.com",
			"Consultation",
			DateTime.UtcNow,
			60,
			null);

		Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
		Assert.Equal("Jane Smith", appointment.CustomerName);
		Assert.Equal(60, appointment.DurationMinutes);
	}

	[Fact]
	public void Should_reject_non_positive_duration()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new Appointment(
				Guid.NewGuid(),
				"Jane Smith",
				"0821234567",
				null,
				"Consultation",
				DateTime.UtcNow,
				0,
				null));
	}

	[Fact]
	public void Should_cancel_appointment()
	{
		var appointment = CreateAppointment();

		appointment.Cancel();

		Assert.Equal(
			AppointmentStatus.Cancelled,
			appointment.Status);
	}

	[Fact]
	public void Should_reschedule_appointment()
	{
		var appointment = CreateAppointment();
		var newDate = DateTime.UtcNow.AddDays(2);

		appointment.Reschedule(newDate, 90);

		Assert.Equal(newDate, appointment.AppointmentDateUtc);
		Assert.Equal(90, appointment.DurationMinutes);
	}

	private static Appointment CreateAppointment() =>
		new(
			Guid.NewGuid(),
			"Jane Smith",
			"0821234567",
			null,
			"Consultation",
			DateTime.UtcNow,
			60,
			null);
}