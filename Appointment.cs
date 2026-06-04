namespace DoctorBookingApp.API.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public int DoctorId { get; set; }
        public int SlotId { get; set; }
        public string AppointmentMode { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
        public DateTime BookingDate { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? VideoLink { get; set; }
        public string? ClinicAddress { get; set; }
        public string? Notes { get; set; }
    }
}
