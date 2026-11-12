using GYM.Models;
using GYM.Models.QuestModels;
using Microsoft.EntityFrameworkCore;
using GYM.Models.ChatModels;
using System.Reflection.Emit;

namespace GYM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Configures foreign-key relationships with Restrict delete behavior (prevents cascading
        // deletes across Users/Slots/Appointments/Quests/ChatMessages) so related rows must be
        // cleaned up manually before a referenced row can be removed.
        public DbSet<User> Users { get; set; }
        public DbSet<AvailableSlot> AvailableSlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<GymInfo> GymInfos { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<GymPhoto> GymPhotos { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<QuestCompletion> QuestCompletions { get; set; }
        public DbSet<TrainerQuest> TrainerQuests { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<GymOperatingHour> GymOperatingHours { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AvailableSlot>()
                .HasOne(a => a.Trainer)
                .WithMany()
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Member)
                .WithMany()
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Slot)
                .WithMany()
                .HasForeignKey(a => a.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainerQuest>()
                .HasOne(tq => tq.Trainer)
                .WithMany()
                .HasForeignKey(tq => tq.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainerQuest>()
                .HasOne(tq => tq.Member)
                .WithMany()
                .HasForeignKey(tq => tq.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}