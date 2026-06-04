using DoctorBookingApp.API.Data;
using DoctorBookingApp.API.DTOs;
using DoctorBookingApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DoctorBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly DatabaseHelper _db;
        private readonly EmailService _emailService;

        public AppointmentsController(DatabaseHelper db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto dto)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Check if slot is available
                using var checkSlotCmd = new MySqlCommand("SELECT IsBooked, SlotDate FROM DoctorSlots WHERE SlotId = @slotId AND DoctorId = @docId FOR UPDATE", conn, transaction);
                checkSlotCmd.Parameters.AddWithValue("@slotId", dto.SlotId);
                checkSlotCmd.Parameters.AddWithValue("@docId", dto.DoctorId);
                
                using var reader = await checkSlotCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return BadRequest("Slot not found.");
                
                if (reader.GetBoolean(0)) return BadRequest("Slot is already booked.");
                var slotDate = reader.GetDateTime(1);
                await reader.DisposeAsync();

                // Get Doctor Info
                using var docCmd = new MySqlCommand("SELECT ConsultationMode, ClinicAddress, Email, DoctorName FROM Doctors WHERE DoctorId = @docId", conn, transaction);
                docCmd.Parameters.AddWithValue("@docId", dto.DoctorId);
                using var docReader = await docCmd.ExecuteReaderAsync();
                await docReader.ReadAsync();
                var mode = docReader.GetString(0);
                var address = docReader.IsDBNull(1) ? null : docReader.GetString(1);
                var docEmail = docReader.GetString(2);
                var docName = docReader.GetString(3);
                await docReader.DisposeAsync();

                // Create Appointment
                using var cmd = new MySqlCommand(@"
                    INSERT INTO Appointments (UserId, DoctorId, SlotId, AppointmentMode, Status, AppointmentDate, ClinicAddress, Notes)
                    VALUES (@uId, @dId, @sId, @mode, 'Confirmed', @date, @addr, @notes);
                    SELECT LAST_INSERT_ID();", conn, transaction);
                
                cmd.Parameters.AddWithValue("@uId", userId);
                cmd.Parameters.AddWithValue("@dId", dto.DoctorId);
                cmd.Parameters.AddWithValue("@sId", dto.SlotId);
                cmd.Parameters.AddWithValue("@mode", mode);
                cmd.Parameters.AddWithValue("@date", slotDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@addr", (object?)address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)dto.Notes ?? DBNull.Value);
                var appId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // Mark Slot Booked
                using var updateSlotCmd = new MySqlCommand("UPDATE DoctorSlots SET IsBooked = TRUE WHERE SlotId = @slotId", conn, transaction);
                updateSlotCmd.Parameters.AddWithValue("@slotId", dto.SlotId);
                await updateSlotCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                // Email Doctor
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                await _emailService.SendEmailAsync(docEmail, "New Appointment Booking", $"You have a new appointment booking from {userName} on {slotDate:yyyy-MM-dd}. Please login to confirm and send details.");

                return Ok(new { message = "Appointment booked successfully.", appointmentId = appId });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet("patient")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetPatientAppointments()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var result = new List<AppointmentSummaryDto>();

            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT a.AppointmentId, u.FullName AS PatientName, d.DoctorName, s.SpecialtyName, 
                       a.AppointmentMode, a.Status, a.AppointmentDate, ds.SlotStartTime, ds.SlotEndTime,
                       a.VideoLink, a.ClinicAddress, a.Notes
                FROM Appointments a
                JOIN Users u ON a.UserId = u.UserId
                JOIN Doctors d ON a.DoctorId = d.DoctorId
                JOIN Specialties s ON d.SpecialtyId = s.SpecialtyId
                JOIN DoctorSlots ds ON a.SlotId = ds.SlotId
                WHERE a.UserId = @userId
                ORDER BY a.AppointmentDate DESC, ds.SlotStartTime DESC", conn);
            
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(MapToDto(reader));
            }
            return Ok(result);
        }

        [HttpGet("doctor")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDoctorAppointments()
        {
            var docId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var result = new List<AppointmentSummaryDto>();

            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT a.AppointmentId, u.FullName AS PatientName, d.DoctorName, s.SpecialtyName, 
                       a.AppointmentMode, a.Status, a.AppointmentDate, ds.SlotStartTime, ds.SlotEndTime,
                       a.VideoLink, a.ClinicAddress, a.Notes
                FROM Appointments a
                JOIN Users u ON a.UserId = u.UserId
                JOIN Doctors d ON a.DoctorId = d.DoctorId
                JOIN Specialties s ON d.SpecialtyId = s.SpecialtyId
                JOIN DoctorSlots ds ON a.SlotId = ds.SlotId
                WHERE a.DoctorId = @docId
                ORDER BY a.AppointmentDate DESC, ds.SlotStartTime DESC", conn);
            
            cmd.Parameters.AddWithValue("@docId", docId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(MapToDto(reader));
            }
            return Ok(result);
        }

        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> ConfirmAppointment(int id, [FromBody] string? videoLink)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("UPDATE Appointments SET Status = 'Confirmed', VideoLink = @link WHERE AppointmentId = @id AND DoctorId = @docId", conn);
            cmd.Parameters.AddWithValue("@link", (object?)videoLink ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@docId", int.Parse(User.FindFirst("id")?.Value ?? "0"));

            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) return NotFound("Appointment not found or unauthorized.");

            // Get Patient Email
            using var pCmd = new MySqlCommand("SELECT u.Email, a.AppointmentMode, a.ClinicAddress FROM Appointments a JOIN Users u ON a.UserId = u.UserId WHERE a.AppointmentId = @id", conn);
            pCmd.Parameters.AddWithValue("@id", id);
            using var pReader = await pCmd.ExecuteReaderAsync();
            if (await pReader.ReadAsync())
            {
                var pEmail = pReader.GetString(0);
                var mode = pReader.GetString(1);
                var addr = pReader.IsDBNull(2) ? "" : pReader.GetString(2);
                
                var body = $"Your appointment has been confirmed. <br>Mode: {mode}<br>";
                if (mode == "Online" && !string.IsNullOrEmpty(videoLink)) body += $"Video Link: <a href='{videoLink}'>{videoLink}</a>";
                if (mode == "Offline") body += $"Clinic Address: {addr}<br>Your Patient ID: {id}";
                
                await _emailService.SendEmailAsync(pEmail, "Appointment Confirmed", body);
            }

            return Ok(new { message = "Appointment confirmed successfully." });
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var query = "UPDATE Appointments SET Status = 'Cancelled' WHERE AppointmentId = @id AND ";
            query += role == "Doctor" ? "DoctorId = @uId" : "UserId = @uId";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@uId", userId);
            
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) return NotFound();

            // Free slot
            using var slotCmd = new MySqlCommand("UPDATE DoctorSlots SET IsBooked = FALSE WHERE SlotId = (SELECT SlotId FROM Appointments WHERE AppointmentId = @id)", conn);
            slotCmd.Parameters.AddWithValue("@id", id);
            await slotCmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Appointment cancelled." });
        }

        [HttpPut("{id}/complete")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("UPDATE Appointments SET Status = 'Completed' WHERE AppointmentId = @id AND DoctorId = @docId", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@docId", int.Parse(User.FindFirst("id")?.Value ?? "0"));
            
            await cmd.ExecuteNonQueryAsync();
            return Ok(new { message = "Appointment marked as completed." });
        }

        [HttpPut("{id}/no-show")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("UPDATE Appointments SET Status = 'NoShow' WHERE AppointmentId = @id AND DoctorId = @docId", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@docId", int.Parse(User.FindFirst("id")?.Value ?? "0"));
            
            await cmd.ExecuteNonQueryAsync();
            return Ok(new { message = "Appointment marked as no-show." });
        }

        private static AppointmentSummaryDto MapToDto(MySqlDataReader reader)
        {
            return new AppointmentSummaryDto
            {
                AppointmentId = reader.GetInt32(0),
                PatientName = reader.GetString(1),
                DoctorName = reader.GetString(2),
                SpecialtyName = reader.GetString(3),
                AppointmentMode = reader.GetString(4),
                Status = reader.GetString(5),
                AppointmentDate = reader.GetDateTime(6),
                SlotStartTime = reader.GetTimeSpan(7),
                SlotEndTime = reader.GetTimeSpan(8),
                VideoLink = reader.IsDBNull(9) ? null : reader.GetString(9),
                ClinicAddress = reader.IsDBNull(10) ? null : reader.GetString(10),
                Notes = reader.IsDBNull(11) ? null : reader.GetString(11)
            };
        }
    }
}
