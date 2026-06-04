using DoctorBookingApp.API.Data;
using DoctorBookingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DoctorBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public FeedbackController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            feedback.PatientId = userId;

            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var checkCmd = new MySqlCommand("SELECT Status FROM Appointments WHERE AppointmentId = @appId AND UserId = @userId", conn);
            checkCmd.Parameters.AddWithValue("@appId", feedback.AppointmentId);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            
            var status = (string?)await checkCmd.ExecuteScalarAsync();
            if (status != "Completed") return BadRequest("Feedback can only be submitted for completed appointments.");

            using var cmd = new MySqlCommand(@"
                INSERT INTO Feedback (AppointmentId, PatientId, DoctorId, Rating, Comments)
                VALUES (@aId, @pId, @dId, @rating, @comments)", conn);
            
            cmd.Parameters.AddWithValue("@aId", feedback.AppointmentId);
            cmd.Parameters.AddWithValue("@pId", feedback.PatientId);
            cmd.Parameters.AddWithValue("@dId", feedback.DoctorId);
            cmd.Parameters.AddWithValue("@rating", feedback.Rating);
            cmd.Parameters.AddWithValue("@comments", (object?)feedback.Comments ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return Ok(new { message = "Feedback submitted successfully." });
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetDoctorFeedback(int doctorId)
        {
            var feedbackList = new List<object>();
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT f.Rating, f.Comments, u.FullName, f.CreatedAt 
                FROM Feedback f
                JOIN Users u ON f.PatientId = u.UserId
                WHERE f.DoctorId = @docId
                ORDER BY f.CreatedAt DESC", conn);
            
            cmd.Parameters.AddWithValue("@docId", doctorId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                feedbackList.Add(new
                {
                    Rating = reader.GetInt32(0),
                    Comments = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PatientName = reader.GetString(2),
                    Date = reader.GetDateTime(3)
                });
            }

            return Ok(feedbackList);
        }
    }
}
