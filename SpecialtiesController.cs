using DoctorBookingApp.API.Data;
using DoctorBookingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DoctorBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpecialtiesController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public SpecialtiesController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetSpecialties()
        {
            var specialties = new List<Specialty>();
            await using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SELECT SpecialtyId, SpecialtyName FROM Specialties ORDER BY SpecialtyName", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                specialties.Add(new Specialty
                {
                    SpecialtyId = reader.GetInt32(0),
                    SpecialtyName = reader.GetString(1)
                });
            }

            return Ok(specialties);
        }
    }
}
