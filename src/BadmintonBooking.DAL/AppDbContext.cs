using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User indexes & config
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue(AppRoles.Player);
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Facility config
        modelBuilder.Entity<Facility>(entity =>
        {
            entity.HasOne(f => f.Provider)
                  .WithMany(u => u.Facilities)
                  .HasForeignKey(f => f.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(f => f.Status).HasDefaultValue(FacilityStatuses.Pending);
            entity.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Court config
        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasOne(c => c.Facility)
                  .WithMany(f => f.Courts)
                  .HasForeignKey(c => c.FacilityId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(c => c.CourtType).HasDefaultValue(CourtTypes.PVC);
            entity.Property(c => c.Status).HasDefaultValue(CourtStatuses.Active);
        });

        // TimeSlot config
        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasOne(ts => ts.Facility)
                  .WithMany(f => f.TimeSlots)
                  .HasForeignKey(ts => ts.FacilityId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ts => ts.IsActive).HasDefaultValue(true);
            entity.Property(ts => ts.Price).HasPrecision(18, 2);
        });

        // Booking config
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(b => b.Player)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(b => b.PlayerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Court)
                  .WithMany(c => c.Bookings)
                  .HasForeignKey(b => b.CourtId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.TimeSlot)
                  .WithMany(ts => ts.Bookings)
                  .HasForeignKey(b => b.TimeSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(b => b.Status).HasDefaultValue(BookingStatuses.Pending);
            entity.Property(b => b.TotalPrice).HasPrecision(18, 2);
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Wallet config
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasOne(w => w.User)
                  .WithOne(u => u.Wallet)
                  .HasForeignKey<Wallet>(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(w => w.UserId).IsUnique();
            entity.Property(w => w.Balance).HasPrecision(18, 2).HasDefaultValue(0);
            entity.Property(w => w.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // WalletTransaction config
        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasOne(t => t.Wallet)
                  .WithMany(w => w.Transactions)
                  .HasForeignKey(t => t.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.TransactionType).HasDefaultValue(WalletTransactionTypes.Deposit);
            entity.Property(t => t.Status).HasDefaultValue(WalletTransactionStatuses.Pending);
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(t => t.ReferenceCode);
        });
    }
}
