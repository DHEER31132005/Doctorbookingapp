using System.ComponentModel.DataAnnotations;

namespace DoctorBookingApp.API.DTOs
{
    public class PatientRegisterDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class PatientLoginDto
    {
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class DoctorRegisterDto
    {
        [Required] public string DoctorName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required] public int SpecialtyId { get; set; }
        [Required] public string ConsultationMode { get; set; } = string.Empty; // Online or Offline
        [Required] public decimal ConsultationFee { get; set; }
        public int ExperienceYears { get; set; }
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        public string? ClinicAddress { get; set; } // Required if offline
    }

    public class DoctorLoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required] public string SecretKey { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Token { get; set; } = string.Empty;
        [Required] public string NewPassword { get; set; } = string.Empty;
    }

    public class RecoverSecretKeyDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public object User { get; set; } = null!;
    }
}
