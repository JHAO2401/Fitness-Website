using GYM.Data;
using GYM.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM.Helpers
{
    public static class OperatingHoursHelper
    {
        private static readonly string[] DayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        // Builds a human-readable summary like "Mon - Fri: 6:00 AM - 11:00 PM, Sat - Sun: 8:00 AM - 9:00 PM"
        // by grouping consecutive days that share the same schedule.
        public static string BuildSummaryText(List<GymOperatingHour> hours)
        {
            if (hours == null || hours.Count == 0) return "Not set";

            var ordered = hours.OrderBy(h => h.DayOfWeek).ToList();
            var groups = new List<(string Days, string TimeText)>();

            int i = 0;
            while (i < ordered.Count)
            {
                var current = ordered[i];
                string timeText = current.IsClosed ? "Closed" : $"{FormatTime(current.OpenTime)} - {FormatTime(current.CloseTime)}";

                int start = i;
                int end = i;

                while (end + 1 < ordered.Count)
                {
                    var next = ordered[end + 1];
                    string nextText = next.IsClosed ? "Closed" : $"{FormatTime(next.OpenTime)} - {FormatTime(next.CloseTime)}";
                    if (nextText == timeText)
                    {
                        end++;
                    }
                    else break;
                }

                string dayLabel = start == end
                    ? DayNames[ordered[start].DayOfWeek]
                    : $"{DayNames[ordered[start].DayOfWeek]} - {DayNames[ordered[end].DayOfWeek]}";

                groups.Add((dayLabel, timeText));
                i = end + 1;
            }

            return string.Join("\n", groups.Select(g => $"{g.Days}: {g.TimeText}"));
        }

        // Formats a TimeSpan as "h:mm tt" (e.g. "6:00 AM")
        private static string FormatTime(TimeSpan t)
        {
            var dt = DateTime.Today.Add(t);
            return dt.ToString("h:mm tt");
        }

        // Determines whether the gym is open right now and builds a status message (e.g. "Open now · Closes 11:00 PM")

        public static (bool isOpenNow, string statusText) GetCurrentStatus(List<GymOperatingHour> hours)
        {
            if (hours == null || hours.Count == 0)
            {
                return (false, "Hours not set");
            }

            int todayIndex = (int)DateTime.Now.DayOfWeek; // 0=Sunday ... 6=Saturday
            var today = hours.FirstOrDefault(h => h.DayOfWeek == todayIndex);

            if (today == null || today.IsClosed)
            {
                return (false, "Closed");
            }

            var now = DateTime.Now.TimeOfDay;
            bool isOpenNow = now >= today.OpenTime && now < today.CloseTime;

            return isOpenNow
                ? (true, $"Open now · Closes {FormatTime(today.CloseTime)}")
                : (false, $"Closed · Opens {FormatTime(today.OpenTime)}");
        }
        // Returns (isOpen, openTime, closeTime) for a given day of week (0=Sunday..6=Saturday)
        public static async Task<(bool isOpen, TimeSpan openTime, TimeSpan closeTime)> GetHoursForDayAsync(ApplicationDbContext context, int dayOfWeek)
        {
            var hour = await context.GymOperatingHours.FirstOrDefaultAsync(h => h.DayOfWeek == dayOfWeek);

            if (hour == null || hour.IsClosed)
            {
                return (false, TimeSpan.Zero, TimeSpan.Zero);
            }

            return (true, hour.OpenTime, hour.CloseTime);
        }
    }
}