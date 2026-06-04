namespace DoctorBookingApp.API.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public MySqlConnector.MySqlConnection GetConnection()
        {
            return new MySqlConnector.MySqlConnection(_connectionString);
        }
    }
}
