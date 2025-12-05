using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Restaurant configuration
        builder.Entity<Restaurant>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        // Branch configuration
        builder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.RestaurantId);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            
            entity.HasOne(e => e.Restaurant)
                .WithMany(r => r.Branches)
                .HasForeignKey(e => e.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Table configuration
        builder.Entity<Table>(entity =>
        {
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => new { e.BranchId, e.TableNumber }).IsUnique();
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Tables)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TimeSlot configuration
        builder.Entity<TimeSlot>(entity =>
        {
            entity.HasIndex(e => e.BranchId);
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.TimeSlots)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Booking configuration
        builder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.BookingReference).IsUnique();
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.TableId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BookingDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.BranchId, e.BookingDate, e.Status });
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Bookings)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Table)
                .WithMany(t => t.Bookings)
                .HasForeignKey(e => e.TableId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Coupon)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.CouponId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Menu configuration
        builder.Entity<Menu>(entity =>
        {
            entity.HasIndex(e => e.RestaurantId);
            entity.HasIndex(e => e.BranchId);
            
            entity.HasOne(e => e.Restaurant)
                .WithMany(r => r.Menus)
                .HasForeignKey(e => e.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Menus)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuCategory configuration
        builder.Entity<MenuCategory>(entity =>
        {
            entity.HasIndex(e => e.MenuId);
            
            entity.HasOne(e => e.Menu)
                .WithMany(m => m.Categories)
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MenuItem configuration
        builder.Entity<MenuItem>(entity =>
        {
            entity.HasIndex(e => e.CategoryId);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Review configuration
        builder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BookingId).IsUnique();
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Reviews)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(e => e.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Offer configuration
        builder.Entity<Offer>(entity =>
        {
            entity.HasIndex(e => e.RestaurantId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            
            entity.HasOne(e => e.Restaurant)
                .WithMany(r => r.Offers)
                .HasForeignKey(e => e.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.Offers)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Coupon configuration
        builder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // LoyaltyTransaction configuration
        builder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.LoyaltyTransactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });

        // Payment configuration
        builder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
            
            entity.HasOne(e => e.Booking)
                .WithMany()
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUser additional configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(e => e.Restaurant)
                .WithMany()
                .HasForeignKey(e => e.RestaurantId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
