namespace DoctorBookingApp.API.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
