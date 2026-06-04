using DoctorBookingApp.API.Data;
using DoctorBookingApp.API.DTOs;
using DoctorBookingApp.API.Models;
using DoctorBookingApp.API.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DoctorBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DatabaseHelper _db;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;

        public AuthController(DatabaseHelper db, JwtService jwtService, EmailService emailService)
        {
            _db = db;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [HttpPost("patient/register")]
        public async Task<IActionResult> RegisterPatient([FromBody] PatientRegisterDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check if user exists
            using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @email OR PhoneNumber = @phone", conn);
            checkCmd.Parameters.AddWithValue("@email", dto.Email);
            checkCmd.Parameters.AddWithValue("@phone", dto.PhoneNumber);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            
            if (exists) return BadRequest(new { message = "User with this email or phone already exists." });

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            using var cmd = new MySqlCommand(@"
                INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role) 
                VALUES (@name, @email, @hash, @phone, 'Patient');
                SELECT LAST_INSERT_ID();", conn);
            
            cmd.Parameters.AddWithValue("@name", dto.FullName);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@phone", dto.PhoneNumber);

            var userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("patient/login")]
        public async Task<IActionResult> LoginPatient([FromBody] PatientLoginDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT UserId, FullName, Email, PasswordHash, Role FROM Users WHERE PhoneNumber = @phone", conn);
            cmd.Parameters.AddWithValue("@phone", dto.PhoneNumber);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return Unauthorized(new { message = "Invalid phone number or password." });

            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var email = reader.GetString(2);
            var hash = reader.GetString(3);
            var role = reader.GetString(4);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash))
                return Unauthorized(new { message = "Invalid phone number or password." });

            var token = _jwtService.GenerateToken(id, email, role, name);

            return Ok(new AuthResponseDto 
            { 
                Token = token, 
                User = new { Id = id, Name = name, Email = email, Role = role } 
            });
        }

        [HttpPost("doctor/register")]
        public async Task<IActionResult> RegisterDoctor([FromBody] DoctorRegisterDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Check email exists
                using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM DoctorAuth WHERE Email = @email", conn, transaction);
                checkCmd.Parameters.AddWithValue("@email", dto.Email);
                if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                    return BadRequest(new { message = "Doctor with this email already exists." });

                // Insert into Doctors
                using var cmdDoc = new MySqlCommand(@"
                    INSERT INTO Doctors (DoctorName, SpecialtyId, ConsultationMode, ConsultationFee, ExperienceYears, Email, PhoneNumber, ClinicAddress)
                    VALUES (@name, @spec, @mode, @fee, @exp, @email, @phone, @addr);
                    SELECT LAST_INSERT_ID();", conn, transaction);
                
                cmdDoc.Parameters.AddWithValue("@name", dto.DoctorName);
                cmdDoc.Parameters.AddWithValue("@spec", dto.SpecialtyId);
                cmdDoc.Parameters.AddWithValue("@mode", dto.ConsultationMode);
                cmdDoc.Parameters.AddWithValue("@fee", dto.ConsultationFee);
                cmdDoc.Parameters.AddWithValue("@exp", dto.ExperienceYears);
                cmdDoc.Parameters.AddWithValue("@email", dto.Email);
                cmdDoc.Parameters.AddWithValue("@phone", dto.PhoneNumber);
                cmdDoc.Parameters.AddWithValue("@addr", (object?)dto.ClinicAddress ?? DBNull.Value);

                var docId = Convert.ToInt32(await cmdDoc.ExecuteScalarAsync());

                // Generate Secret Key
                var secretKey = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                // Insert into DoctorAuth
                using var cmdAuth = new MySqlCommand(@"
                    INSERT INTO DoctorAuth (DoctorId, Email, PasswordHash, SecretKey)
                    VALUES (@docId, @email, @hash, @secret);", conn, transaction);
                
                cmdAuth.Parameters.AddWithValue("@docId", docId);
                cmdAuth.Parameters.AddWithValue("@email", dto.Email);
                cmdAuth.Parameters.AddWithValue("@hash", hash);
                cmdAuth.Parameters.AddWithValue("@secret", secretKey);

                await cmdAuth.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

                // Send email with secret key
                var body = $"Hello Dr. {dto.DoctorName},<br>Your registration is successful. Your Secret Key is: <b>{secretKey}</b><br>Please keep this safe, you need it to log in.";
                await _emailService.SendEmailAsync(dto.Email, "Doctor Registration Successful", body);

                return Ok(new { message = "Registration successful. Secret key has been emailed to you." });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost("doctor/login")]
        public async Task<IActionResult> LoginDoctor([FromBody] DoctorLoginDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT d.DoctorId, d.DoctorName, a.PasswordHash, a.SecretKey 
                FROM DoctorAuth a
                JOIN Doctors d ON a.DoctorId = d.DoctorId
                WHERE a.Email = @email", conn);
            
            cmd.Parameters.AddWithValue("@email", dto.Email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return Unauthorized(new { message = "Invalid credentials." });

            var docId = reader.GetInt32(0);
            var name = reader.GetString(1);
            var hash = reader.GetString(2);
            var secret = reader.GetString(3);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash) || dto.SecretKey != secret)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _jwtService.GenerateToken(docId, dto.Email, "Doctor", name);

            return Ok(new AuthResponseDto 
            { 
                Token = token, 
                User = new { Id = docId, Name = name, Email = dto.Email, Role = "Doctor" } 
            });
        }
        
        [HttpPost("patient/forgot-password")]
        public async Task<IActionResult> PatientForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT UserId FROM Users WHERE Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            var userId = await cmd.ExecuteScalarAsync();

            if (userId == null) return Ok(new { message = "If the email exists, a reset link will be sent." });

            var token = Guid.NewGuid().ToString();
            var expiry = DateTime.Now.AddHours(1);

            using var updateCmd = new MySqlCommand("UPDATE Users SET PasswordResetToken = @token, ResetTokenExpiry = @expiry WHERE Email = @email", conn);
            updateCmd.Parameters.AddWithValue("@token", token);
            updateCmd.Parameters.AddWithValue("@expiry", expiry);
            updateCmd.Parameters.AddWithValue("@email", dto.Email);
            await updateCmd.ExecuteNonQueryAsync();

            await _emailService.SendEmailAsync(dto.Email, "Password Reset", $"Your password reset token is: {token}");

            return Ok(new { message = "If the email exists, a reset link will be sent." });
        }

        [HttpPost("patient/reset-password")]
        public async Task<IActionResult> PatientResetPassword([FromBody] ResetPasswordDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT UserId FROM Users WHERE Email = @email AND PasswordResetToken = @token AND ResetTokenExpiry > NOW()", conn);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            cmd.Parameters.AddWithValue("@token", dto.Token);
            
            var userId = await cmd.ExecuteScalarAsync();
            if (userId == null) return BadRequest(new { message = "Invalid or expired token." });

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            using var updateCmd = new MySqlCommand("UPDATE Users SET PasswordHash = @hash, PasswordResetToken = NULL, ResetTokenExpiry = NULL WHERE Email = @email", conn);
            updateCmd.Parameters.AddWithValue("@hash", hash);
            updateCmd.Parameters.AddWithValue("@email", dto.Email);
            await updateCmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Password reset successfully." });
        }
        
        [HttpPost("doctor/forgot-password")]
        public async Task<IActionResult> DoctorForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT DoctorId FROM DoctorAuth WHERE Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            var docId = await cmd.ExecuteScalarAsync();

            if (docId == null) return Ok(new { message = "If the email exists, a reset link will be sent." });

            var token = Guid.NewGuid().ToString();
            var expiry = DateTime.Now.AddHours(1);

            using var updateCmd = new MySqlCommand("UPDATE DoctorAuth SET PasswordResetToken = @token, ResetTokenExpiry = @expiry WHERE Email = @email", conn);
            updateCmd.Parameters.AddWithValue("@token", token);
            updateCmd.Parameters.AddWithValue("@expiry", expiry);
            updateCmd.Parameters.AddWithValue("@email", dto.Email);
            await updateCmd.ExecuteNonQueryAsync();

            await _emailService.SendEmailAsync(dto.Email, "Password Reset", $"Your password reset token is: {token}");

            return Ok(new { message = "If the email exists, a reset link will be sent." });
        }
        
        [HttpPost("doctor/reset-password")]
        public async Task<IActionResult> DoctorResetPassword([FromBody] ResetPasswordDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT DoctorId FROM DoctorAuth WHERE Email = @email AND PasswordResetToken = @token AND ResetTokenExpiry > NOW()", conn);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            cmd.Parameters.AddWithValue("@token", dto.Token);
            
            var docId = await cmd.ExecuteScalarAsync();
            if (docId == null) return BadRequest(new { message = "Invalid or expired token." });

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            using var updateCmd = new MySqlCommand("UPDATE DoctorAuth SET PasswordHash = @hash, PasswordResetToken = NULL, ResetTokenExpiry = NULL WHERE Email = @email", conn);
            updateCmd.Parameters.AddWithValue("@hash", hash);
            updateCmd.Parameters.AddWithValue("@email", dto.Email);
            await updateCmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpPost("doctor/recover-secret")]
        public async Task<IActionResult> DoctorRecoverSecret([FromBody] RecoverSecretKeyDto dto)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT PasswordHash, SecretKey FROM DoctorAuth WHERE Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", dto.Email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return BadRequest(new { message = "Invalid credentials." });

            var hash = reader.GetString(0);
            var secret = reader.GetString(1);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash))
                return BadRequest(new { message = "Invalid credentials." });

            await _emailService.SendEmailAsync(dto.Email, "Secret Key Recovery", $"Your secret key is: {secret}");

            return Ok(new { message = "Secret key has been emailed to you." });
        }
    }
}
