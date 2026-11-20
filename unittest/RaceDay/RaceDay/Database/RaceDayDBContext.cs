using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using Results = RaceDay.Models.Results;

namespace RaceDay.Database
{
    public class RaceDayDBContext : DbContext
    {
        public RaceDayDBContext(DbContextOptions<RaceDayDBContext> options) : base(options) { }

        public DbSet<Participant> Participant { get; set; }
        public DbSet<Organiser> Organiser { get; set; }
        public DbSet<Location> Location { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Enrolment> Enrolment { get; set; }
        public DbSet<Results> Results { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Fix for SQL Server Error 1785 (Multiple Cascade Paths)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany()
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix for Event -> Category -> Enrolment vs Event -> Enrolment conflict
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.@event)
                .WithMany()
                .HasForeignKey(e => e.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.category)
                .WithMany()
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Organiser>()
                .HasIndex(o => o.Name)
                .IsUnique();

            modelBuilder.Entity<Participant>()
                .HasIndex(o => o.Name)
                .IsUnique();
        }
}
}
