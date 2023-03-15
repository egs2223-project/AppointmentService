using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace AppointmentService
{
    public class AppointmentDb : DbContext
    {
        public AppointmentDb(DbContextOptions<AppointmentDb> options)
        : base(options) { }

        public DbSet<Appointment> Appointments => Set<Appointment>();

        public DbSet<Participant> Participants => Set<Participant>();

        public DbSet<RecurringOptions> RecurringOptions => Set<RecurringOptions>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Participant>()
            //    .Property(b => b.Id)
            //    .HasDefaultValueSql("newsequentialid()");

            modelBuilder.Entity<Appointment>()
                .Property(b => b.Id)
                .HasDefaultValueSql("newsequentialid()");
        }
    }
}
