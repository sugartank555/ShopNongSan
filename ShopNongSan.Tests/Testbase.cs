using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ShopNongSan.Data;
using ShopNongSan.Models;
using System.Security.Claims;

namespace ShopNongSan.Tests
{
    /// <summary>
    /// Base class dùng chung cho tất cả test classes.
    /// Tạo InMemory DB mới cho mỗi test (IDisposable).
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected ApplicationDbContext Db { get; }

        protected TestBase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // DB riêng mỗi test
                .Options;

            Db = new ApplicationDbContext(options);
            Db.Database.EnsureCreated();
            SeedData();
        }

        // ── Seed dữ liệu mẫu dùng chung ────────────────────────
        private void SeedData()
        {
            // Categories
            Db.Categories.AddRange(
                new Category { Id = 1, Name = "Rau củ", IsActive = true },
                new Category { Id = 2, Name = "Trái cây", IsActive = true },
                new Category { Id = 3, Name = "Inactive", IsActive = false }
            );

            // Products
            Db.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Name = "Cà chua",
                    Price = 25000,
                    CategoryId = 1,
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Dưa hấu",
                    Price = 15000,
                    CategoryId = 2,
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Name = "Sản phẩm ẩn",
                    Price = 5000,
                    CategoryId = 1,
                    IsActive = false,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // BatchLots
            Db.BatchLots.AddRange(
                new BatchLot
                {
                    Id = 1,
                    LotCode = "LOT-001",
                    FarmName = "Trang trại A",
                    HarvestDate = DateTime.Today.AddDays(-10),
                    ExpireDate = DateTime.Today.AddMonths(2),
                    QuantityIn = 100,
                    QuantitySold = 20,
                    ProductId = 1
                },
                new BatchLot
                {
                    Id = 2,
                    LotCode = "LOT-002",
                    FarmName = "Trang trại B",
                    HarvestDate = DateTime.Today.AddDays(-5),
                    ExpireDate = DateTime.Today.AddMonths(1),
                    QuantityIn = 50,
                    QuantitySold = 0,
                    ProductId = 2
                }
            );

            // Coupons
            Db.Coupons.AddRange(
                new Coupon
                {
                    Id = 1,
                    Code = "SALE10",
                    DiscountType = "Percent",
                    DiscountValue = 10,
                    ExpiryDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true
                },
                new Coupon
                {
                    Id = 2,
                    Code = "EXPIRED",
                    DiscountType = "Fixed",
                    DiscountValue = 50000,
                    ExpiryDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new Coupon
                {
                    Id = 3,
                    Code = "INACTIVE",
                    DiscountType = "Percent",
                    DiscountValue = 20,
                    ExpiryDate = DateTime.UtcNow.AddDays(10),
                    IsActive = false
                }
            );

            // Orders
            Db.Orders.AddRange(
                new Order
                {
                    Id = 1,
                    CustomerName = "Nguyễn Văn A",
                    Email = "a@gmail.com",
                    Phone = "0901234567",
                    Status = OrderStatus.Pending,
                    Total = 50000,
                    DiscountAmount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Order
                {
                    Id = 2,
                    CustomerName = "Trần Thị B",
                    Email = "b@gmail.com",
                    Phone = "0907654321",
                    Status = OrderStatus.Paid,
                    Total = 100000,
                    DiscountAmount = 10000,
                    CouponCode = "SALE10",
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                },
                new Order
                {
                    Id = 3,
                    CustomerName = "Lê Văn C",
                    Status = OrderStatus.Completed,
                    Total = 75000,
                    DiscountAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                }
            );

            // OrderItems
            Db.OrderItems.AddRange(
                new OrderItem
                {
                    Id = 1,
                    OrderId = 1,
                    ProductId = 1,
                    Quantity = 2,
                    UnitPrice = 25000
                },
                new OrderItem
                {
                    Id = 2,
                    OrderId = 2,
                    ProductId = 2,
                    Quantity = 1,
                    UnitPrice = 100000
                }
            );

            Db.SaveChanges();
        }

        // ── Helper: gán ControllerContext với user claims ────────
        protected static void SetAdminUser(ControllerBase controller)
            => SetUser(controller, "admin-id", "admin@test.com", "Admin");

        protected static void SetStaffUser(ControllerBase controller)
            => SetUser(controller, "staff-id", "staff@test.com", "Staff");

        protected static void SetCustomerUser(ControllerBase controller)
            => SetUser(controller, "customer-id", "customer@test.com", "Customer");

        private static void SetUser(ControllerBase controller,
            string userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        // ── Helper: tạo UserManager mock ────────────────────────
        protected static Mock<UserManager<IdentityUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        protected static Mock<SignInManager<IdentityUser>> MockSignInManager(
            Mock<UserManager<IdentityUser>> userMgr)
        {
            var ctx = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            return new Mock<SignInManager<IdentityUser>>(
                userMgr.Object, ctx.Object, claimsFactory.Object,
                null!, null!, null!, null!);
        }

        public void Dispose() => Db.Dispose();
    }
}