using Microsoft.EntityFrameworkCore;
using HRS.API.Models;

namespace HRS.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<CustomerModel> Customers { get; set; }
        public DbSet<RoomTypeModel> RoomTypes { get; set; }
        public DbSet<RoomModel> Rooms { get; set; }
        public DbSet<ReservationModel> Reservations { get; set; }
        public DbSet<PaymentModel> Payments { get; set; }
        public DbSet<ChargeModel> Charges { get; set; }
        public DbSet<AuditLogModel> AuditLogs { get; set; }
        public DbSet<SystemSettingsModel> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Room Type pricing
            modelBuilder.Entity<RoomTypeModel>().Property(r => r.BasePrice).HasPrecision(18, 2);
            
            // Room pricing fields
            modelBuilder.Entity<RoomModel>().Property(r => r.RoomSize).HasPrecision(18, 2);
            modelBuilder.Entity<RoomModel>().Property(r => r.ExtraBedPrice).HasPrecision(18, 2);
            modelBuilder.Entity<RoomModel>().Property(r => r.BasePricePerNight).HasPrecision(18, 2);
            modelBuilder.Entity<RoomModel>().Property(r => r.WeekendPrice).HasPrecision(18, 2);
            modelBuilder.Entity<RoomModel>().Property(r => r.HolidayPrice).HasPrecision(18, 2);
            
            // Other entity pricing
            modelBuilder.Entity<ReservationModel>().Property(r => r.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentModel>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<ChargeModel>().Property(c => c.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<SystemSettingsModel>().Property(s => s.TaxRate).HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}
