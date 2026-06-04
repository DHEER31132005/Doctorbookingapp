namespace DoctorBookingApp.API.Models
{
    public class DoctorSlot
    {
        public int SlotId { get; set; }
        public int DoctorId { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan SlotStartTime { get; set; }
        public TimeSpan SlotEndTime { get; set; }
        public bool IsBooked { get; set; }
    }
}
