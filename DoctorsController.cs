using DoctorBookingApp.API.Data;
using DoctorBookingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DoctorBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public DoctorsController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors([FromQuery] int? specialtyId, [FromQuery] string? mode)
        {
            var doctors = new List<object>();
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var query = @"
                SELECT d.DoctorId, d.DoctorName, s.SpecialtyName, d.ConsultationMode, 
                       d.ConsultationFee, d.ExperienceYears, d.ClinicAddress
                FROM Doctors d
                JOIN Specialties s ON d.SpecialtyId = s.SpecialtyId
                WHERE d.Status = 'Active'";
            
            if (specialtyId.HasValue) query += " AND d.SpecialtyId = @specId";
            if (!string.IsNullOrEmpty(mode)) query += " AND d.ConsultationMode = @mode";

            using var cmd = new MySqlCommand(query, conn);
            if (specialtyId.HasValue) cmd.Parameters.AddWithValue("@specId", specialtyId.Value);
            if (!string.IsNullOrEmpty(mode)) cmd.Parameters.AddWithValue("@mode", mode);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                doctors.Add(new
                {
                    DoctorId = reader.GetInt32(0),
                    DoctorName = reader.GetString(1),
                    SpecialtyName = reader.GetString(2),
                    ConsultationMode = reader.GetString(3),
                    ConsultationFee = reader.GetDecimal(4),
                    ExperienceYears = reader.GetInt32(5),
                    ClinicAddress = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return Ok(doctors);
        }

        [HttpGet("{id}/slots")]
        public async Task<IActionResult> GetDoctorSlots(int id, [FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate)) return BadRequest("Invalid date format.");

            var slots = new List<object>();
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT SlotId, SlotStartTime, SlotEndTime, IsBooked 
                FROM DoctorSlots 
                WHERE DoctorId = @docId AND SlotDate = @date AND IsBooked = FALSE
                ORDER BY SlotStartTime", conn);
            
            cmd.Parameters.AddWithValue("@docId", id);
            cmd.Parameters.AddWithValue("@date", parsedDate.ToString("yyyy-MM-dd"));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                slots.Add(new
                {
                    SlotId = reader.GetInt32(0),
                    StartTime = reader.GetTimeSpan(1).ToString(@"hh\:mm"),
                    EndTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                    IsBooked = reader.GetBoolean(3)
                });
            }

            return Ok(slots);
        }
    }
}
