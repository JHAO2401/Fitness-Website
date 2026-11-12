using System.Globalization;

namespace GYM.Helpers
{
    public class QuestDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ExpReward { get; set; }
        public string Type { get; set; } // "Daily" or "Weekly"
    }

    public static class QuestHelper
    {
        public static List<QuestDefinition> DailyQuests => new()
        {
            new QuestDefinition { Key = "daily_login", Title = "Daily Login", Description = "Log in to the gym app today.", ExpReward = 10, Type = "Daily" },
            new QuestDefinition { Key = "daily_workout", Title = "Complete a Workout", Description = "Finish a training session today.", ExpReward = 15, Type = "Daily" },
            new QuestDefinition { Key = "daily_checkin", Title = "Gym Check-In", Description = "Visit the gym today.", ExpReward = 10, Type = "Daily" },
        };

        public static List<QuestDefinition> WeeklyQuests => new()
        {
            new QuestDefinition { Key = "weekly_3_workouts", Title = "Train 3 Times", Description = "Complete 3 workout sessions this week.", ExpReward = 50, Type = "Weekly" },
            new QuestDefinition { Key = "weekly_book_appt", Title = "Book a Trainer Session", Description = "Book at least one appointment with a trainer this week.", ExpReward = 40, Type = "Weekly" },
        };

        // Finds a quest definition (daily or weekly) by its key
        public static QuestDefinition? FindQuest(string key)
        {
            return DailyQuests.Concat(WeeklyQuests).FirstOrDefault(q => q.Key == key);
        }

        // Returns today's date as a period key, used to track daily quest completion
        public static string GetTodayKey() => DateTime.Today.ToString("yyyy-MM-dd");

        // Returns the current ISO-style week (year + week number) as a period key, used to track weekly quest completion
        public static string GetThisWeekKey()
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{DateTime.Today.Year}-W{week}";
        }
    }
}