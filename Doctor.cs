namespace DoctorBookingApp.API.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int SpecialtyId { get; set; }
        public string ConsultationMode { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public int ExperienceYears { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ClinicAddress { get; set; }
        public string Status { get; set; } = "Active";
    }
}
