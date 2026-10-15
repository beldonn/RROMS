using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RROMS.Models;

namespace RROMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<OperatingHours> OperatingHours => Set<OperatingHours>();
    public DbSet<RestaurantSetting> RestaurantSettings => Set<RestaurantSetting>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ChatbotFaq> ChatbotFaqs => Set<ChatbotFaq>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- RestaurantTable ----
        builder.Entity<RestaurantTable>()
            .HasOne(t => t.DiningArea)
            .WithMany(a => a.Tables)
            .HasForeignKey(t => t.DiningAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- MenuItem ----
        builder.Entity<MenuItem>()
            .HasOne(m => m.MenuCategory)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Promotion ----
        builder.Entity<Promotion>()
            .HasOne(p => p.MenuItem)
            .WithMany(m => m.Promotions)
            .HasForeignKey(p => p.MenuItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---- Reservation ----
        builder.Entity<Reservation>()
            .HasOne(r => r.Customer)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.Table)
            .WithMany(t => t.Reservations)
            .HasForeignKey(r => r.TableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Reservation>()
            .HasOne(r => r.ConfirmedByStaff)
            .WithMany()
            .HasForeignKey(r => r.ConfirmedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Order ----
        builder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Reservation)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Order>()
            .HasOne(o => o.Table)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Order>()
            .HasOne(o => o.HandledByStaff)
            .WithMany()
            .HasForeignKey(o => o.HandledByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- OrderItem ----
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany(m => m.OrderItems)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Payment (1:1 with Order) ----
        builder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Feedback ----
        builder.Entity<Feedback>()
            .HasOne(f => f.Customer)
            .WithMany(u => u.Feedbacks)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Feedback>()
            .HasOne(f => f.Order)
            .WithMany(o => o.Feedbacks)
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Feedback>()
            .HasOne(f => f.Reservation)
            .WithMany(r => r.Feedbacks)
            .HasForeignKey(f => f.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Feedback>()
            .HasOne(f => f.RespondedByStaff)
            .WithMany()
            .HasForeignKey(f => f.RespondedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Notification ----
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Announcement ----
        builder.Entity<Announcement>()
            .HasOne(a => a.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(a => a.CreatedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---- Indexes that matter for the availability engine & reporting ----
        builder.Entity<Reservation>().HasIndex(r => r.ReservationDateTime);
        builder.Entity<Order>().HasIndex(o => o.OrderDate);
        builder.Entity<MenuItem>().HasIndex(m => m.IsAvailable);
    }
}
