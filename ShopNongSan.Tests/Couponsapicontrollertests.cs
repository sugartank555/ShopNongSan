using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Controllers.Api;
using ShopNongSan.Models;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class CouponsApiControllerTests : TestBase
    {
        private CouponsApiController CreateController()
        {
            var ctrl = new CouponsApiController(Db);
            SetAdminUser(ctrl);
            return ctrl;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAllCoupons()
        {
            var result = await CreateController().GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetAll_FilterActiveTrue_Returns2()
        {
            var result = await CreateController().GetAll(isActive: true);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task GetAll_FilterActiveFalse_Returns1()
        {
            var result = await CreateController().GetAll(isActive: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list);
        }

        // ════════════════════════════════════════════════════════
        // VALIDATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Validate_ValidCode_Returns200()
        {
            var result = await CreateController().Validate("SALE10");
            Assert.IsType<OkObjectResult>(result);
        }

        // 🐛 BUG 1 — Sai HTTP status code
        // Mã hết hạn phải trả 422 UnprocessableEntity, test expect 404
        [Fact]
        public async Task Validate_ExpiredCode_Returns404() // ❌ đúng phải là 422
        {
            var result = await CreateController().Validate("EXPIRED");
            Assert.IsType<NotFoundObjectResult>(result); // ❌ thực tế trả UnprocessableEntity
        }

        [Fact]
        public async Task Validate_InactiveCode_Returns422()
        {
            var result = await CreateController().Validate("INACTIVE");
            Assert.IsType<UnprocessableEntityObjectResult>(result);
        }

        [Fact]
        public async Task Validate_CodeNotFound_Returns404()
        {
            var result = await CreateController().Validate("NOTEXIST");
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Validate_LowercaseCode_Returns200()
        {
            var result = await CreateController().Validate("sale10");
            Assert.IsType<OkObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_ValidPercent_Returns201()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "NEW20",
                DiscountType = "Percent",
                DiscountValue = 20,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ValidFixed_Returns201()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "FIXED50K",
                DiscountType = "Fixed",
                DiscountValue = 50000,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_DuplicateCode_Returns409()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "SALE10",
                DiscountType = "Percent",
                DiscountValue = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });

            Assert.IsType<ConflictObjectResult>(result);
        }

        // 🐛 BUG 2 — Boundary không chặt
        // DiscountValue = 100% là hợp lệ (max), nhưng test expect 400
        [Fact]
        public async Task Create_Percent100_Returns400() // ❌ đúng phải là 201
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "FREE100",
                DiscountType = "Percent",
                DiscountValue = 100,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });

            Assert.IsType<BadRequestObjectResult>(result); // ❌ thực tế trả 201
        }

        [Fact]
        public async Task Create_PercentOver100_Returns400()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "INVALID",
                DiscountType = "Percent",
                DiscountValue = 101,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ExpiryDateInPast_Returns400()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "PAST",
                DiscountType = "Percent",
                DiscountValue = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(-1)
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 3 — Thiếu validate
        // Tạo coupon thành công phải verify Code lưu dạng UPPERCASE
        // Nhưng test gửi lowercase và không kiểm tra DB
        [Fact]
        public async Task Create_LowercaseCode_DoesNotVerifyUppercase() // ❌ thiếu assert DB
        {
            await CreateController().Create(new CouponRequest
            {
                Code = "newcode",
                DiscountType = "Fixed",
                DiscountValue = 10000,
                ExpiryDate = DateTime.UtcNow.AddDays(10)
            });

            // Thiếu:
            // var saved = Db.Coupons.FirstOrDefault(c => c.Code == "NEWCODE");
            // Assert.NotNull(saved); // phải lưu UPPERCASE
            Assert.True(true); // ⚠️ không verify gì cả
        }

        [Fact]
        public async Task Create_CodeExactly3Chars_Returns201()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "ABC",
                DiscountType = "Fixed",
                DiscountValue = 10000,
                ExpiryDate = DateTime.UtcNow.AddDays(10)
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_CodeExactly20Chars_Returns201()
        {
            var result = await CreateController().Create(new CouponRequest
            {
                Code = "ABCDEFGHIJ1234567890",
                DiscountType = "Fixed",
                DiscountValue = 10000,
                ExpiryDate = DateTime.UtcNow.AddDays(10)
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            var result = await CreateController().Update(1, new CouponRequest
            {
                Code = "SALE10",
                DiscountType = "Percent",
                DiscountValue = 15,
                ExpiryDate = DateTime.UtcNow.AddDays(60),
                IsActive = true
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // 🐛 BUG 4 — Sai HTTP status code
        // Update id không tồn tại phải trả 404, test expect 400
        [Fact]
        public async Task Update_NotFound_Returns400() // ❌ đúng phải là 404
        {
            var result = await CreateController().Update(999, new CouponRequest
            {
                Code = "X",
                DiscountType = "Fixed",
                DiscountValue = 1000,
                ExpiryDate = DateTime.UtcNow.AddDays(10)
            });

            Assert.IsType<BadRequestObjectResult>(result); // ❌ thực tế trả NotFound
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_CouponUsedInOrder_Returns409()
        {
            var result = await CreateController().Delete(1); // SALE10 dùng trong Order 2

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Delete_UnusedCoupon_Returns200()
        {
            var result = await CreateController().Delete(3); // INACTIVE chưa dùng

            Assert.IsType<OkObjectResult>(result);
            Assert.False(Db.Coupons.Any(c => c.Id == 3));
        }

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            var result = await CreateController().Delete(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_NegativeId_Returns400()
        {
            var result = await CreateController().Delete(-1);
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}