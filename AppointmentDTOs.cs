using System.ComponentModel.DataAnnotations;

namespace DoctorBookingApp.API.DTOs
{
    public class BookAppointmentDto
    {
        [Required] public int DoctorId { get; set; }
        [Required] public int SlotId { get; set; }
        public string? Notes { get; set; }
    }

    public class AppointmentSummaryDto
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string AppointmentMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan SlotStartTime { get; set; }
        public TimeSpan SlotEndTime { get; set; }
        public string? VideoLink { get; set; }
        public string? ClinicAddress { get; set; }
        public string? Notes { get; set; }
    }
}
