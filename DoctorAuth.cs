namespace DoctorBookingApp.API.Models
{
    public class DoctorAuth
    {
        public int DoctorAuthId { get; set; }
        public int DoctorId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
