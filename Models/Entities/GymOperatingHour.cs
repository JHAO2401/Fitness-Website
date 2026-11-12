using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class GymOperatingHour
    {
        [Key]
        public int GymOperatingHourId { get; set; }

        [Required]
        public int DayOfWeek { get; set; }   // 0 = Sunday, 1 = Monday, ... 6 = Saturday

        public bool IsClosed { get; set; } = false;

        public TimeSpan OpenTime { get; set; } = new TimeSpan(6, 0, 0);

        public TimeSpan CloseTime { get; set; } = new TimeSpan(23, 0, 0);
    }
}