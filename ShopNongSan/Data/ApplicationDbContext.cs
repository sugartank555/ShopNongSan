using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<BatchLot> BatchLots => Set<BatchLot>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Coupon> Coupons => Set<Coupon>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Danh mục: unique Slug
            b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();

            // Product -> Category: Restrict
            b.Entity<Product>()
              .HasOne(p => p.Category)
              .WithMany(c => c.Products)
              .HasForeignKey(p => p.CategoryId)
              .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> Order: Cascade
            b.Entity<OrderItem>()
              .HasOne(x => x.Order)
              .WithMany(o => o.Items)
              .HasForeignKey(x => x.OrderId)
              .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> Product: Restrict
            b.Entity<OrderItem>()
              .HasOne(x => x.Product)
              .WithMany()
              .HasForeignKey(x => x.ProductId)
              .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> BatchLot: SetNull
            b.Entity<OrderItem>()
              .HasOne(x => x.BatchLot)
              .WithMany()
              .HasForeignKey(x => x.BatchLotId)
              .OnDelete(DeleteBehavior.SetNull);

            // BatchLot -> Product: Restrict
            b.Entity<BatchLot>()
              .HasOne(bl => bl.Product)
              .WithMany()
              .HasForeignKey(bl => bl.ProductId)
              .OnDelete(DeleteBehavior.Restrict);

            // Optional: Mặc định ngày tạo & chỉ mục
            b.Entity<Product>()
             .Property(p => p.CreatedAt)
             .HasDefaultValueSql("GETUTCDATE()");
            b.Entity<Product>()
             .HasIndex(p => new { p.IsActive, p.IsFeatured });
        }
    }
}
