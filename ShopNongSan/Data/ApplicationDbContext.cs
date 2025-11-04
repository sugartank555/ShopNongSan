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

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();

            // Product -> Category: RESTRICT
            b.Entity<Product>()
              .HasOne(p => p.Category).WithMany(c => c.Products)
              .HasForeignKey(p => p.CategoryId)
              .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> Order: CASCADE
            b.Entity<OrderItem>()
              .HasOne(x => x.Order).WithMany(o => o.Items)
              .HasForeignKey(x => x.OrderId)
              .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> Product: RESTRICT (tránh cascade trùng)
            b.Entity<OrderItem>()
              .HasOne(x => x.Product).WithMany()
              .HasForeignKey(x => x.ProductId)
              .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> BatchLot: SET NULL
            b.Entity<OrderItem>()
              .HasOne(x => x.BatchLot).WithMany()
              .HasForeignKey(x => x.BatchLotId)
              .OnDelete(DeleteBehavior.SetNull);

            // BatchLot -> Product: RESTRICT (tránh đường cascade thứ 2)
            b.Entity<BatchLot>()
              .HasOne(bl => bl.Product).WithMany()
              .HasForeignKey(bl => bl.ProductId)
              .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
