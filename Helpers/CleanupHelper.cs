using GYM.Data;
using Microsoft.EntityFrameworkCore;

namespace GYM.Helpers
{
    public static class CleanupHelper
    {
        // Removes Appointments that have been Cancelled for more than 7 days.
        // Called opportunistically whenever appointment lists are viewed.
        public static async Task RemoveOldCancelledAppointmentsAsync(ApplicationDbContext context)
        {
            var cutoff = DateTime.Now.AddMinutes(-30);

            var oldCancelled = await context.Appointments
                .Where(a => a.Status == "Cancelled" && a.RespondedAt != null && a.RespondedAt < cutoff)
                .ToListAsync();

            if (oldCancelled.Count > 0)
            {
                context.Appointments.RemoveRange(oldCancelled);
                await context.SaveChangesAsync();
            }
        }

        // Deletes Pending appointments whose slot has already passed, freeing up the slot
        public static async Task RemoveExpiredPendingAppointmentsAsync(ApplicationDbContext context)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var currentTime = now.TimeOfDay;

            var expiredPending = await context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Status == "Pending" &&
                            (a.Slot.Date < today || (a.Slot.Date == today && a.Slot.EndTime <= currentTime)))
                .ToListAsync();

            if (expiredPending.Count > 0)
            {
                foreach (var appt in expiredPending)
                {
                    appt.Slot.IsBooked = false; // free up the slot
                }
                context.Appointments.RemoveRange(expiredPending);
                await context.SaveChangesAsync();
            }
        }

        // Marks Confirmed appointments as Completed once their slot time has passed
        public static async Task CompleteExpiredAppointmentsAsync(ApplicationDbContext context)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var currentTime = now.TimeOfDay;

            var expiredConfirmed = await context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Status == "Confirmed" &&
                            (a.Slot.Date < today || (a.Slot.Date == today && a.Slot.EndTime <= currentTime)))
                .ToListAsync();

            if (expiredConfirmed.Count > 0)
            {
                foreach (var appt in expiredConfirmed)
                {
                    appt.Status = "Completed";
                }
                await context.SaveChangesAsync();
            }
        }

        // Removes InProgress trainer quests whose member no longer has an eligible (Confirmed/Completed, upcoming) booking with that trainer
        public static async Task RemoveExpiredTrainerQuestsAsync(ApplicationDbContext context)
        {
            var today = DateTime.Today;

            var inProgressQuests = await context.TrainerQuests
                .Where(q => q.Status == "InProgress")
                .ToListAsync();

            var toRemove = new List<GYM.Models.QuestModels.TrainerQuest>();

            foreach (var quest in inProgressQuests)
            {
                bool stillEligible = await context.Appointments
                    .Include(a => a.Slot)
                    .AnyAsync(a => a.Slot.TrainerId == quest.TrainerId &&
                                   a.MemberId == quest.MemberId &&
                                   (a.Status == "Confirmed" || a.Status == "Completed") &&
                                   a.Slot.Date >= today);

                if (!stillEligible)
                {
                    toRemove.Add(quest);
                }
            }

            if (toRemove.Count > 0)
            {
                context.TrainerQuests.RemoveRange(toRemove);
                await context.SaveChangesAsync();
            }
        }
    }
}