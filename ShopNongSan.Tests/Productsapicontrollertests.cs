using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Controllers.Api;
using ShopNongSan.Models;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class ProductsApiControllerTests : TestBase
    {
        private ProductsApiController CreateController()
        {
            var ctrl = new ProductsApiController(Db);
            SetAdminUser(ctrl);
            return ctrl;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAllProducts()
        {
            var result = await CreateController().GetAll(null, null, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetAll_FilterIsActiveTrue_Returns2()
        {
            var result = await CreateController().GetAll(null, isActive: true, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        // 🐛 BUG 1 — Business logic sai
        // CategoryId=1 có 2 sản phẩm (Cà chua + Sản phẩm ẩn), test expect 1
        [Fact]
        public async Task GetAll_FilterByCategoryId1_Returns1() // ❌ đúng phải là 2
        {
            var result = await CreateController().GetAll(categoryId: 1, null, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list); // ❌ thực tế trả 2
        }

        [Fact]
        public async Task GetAll_FilterIsFeaturedTrue_Returns1()
        {
            var result = await CreateController().GetAll(null, null, isFeatured: true);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list);
        }

        // ════════════════════════════════════════════════════════
        // GET BY ID
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetById_ExistingId_Returns200()
        {
            var result = await CreateController().GetById(1);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var result = await CreateController().GetById(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // 🐛 BUG 2 — Boundary không chặt
        // Id = 0 phải trả 400, nhưng test expect 404
        [Fact]
        public async Task GetById_ZeroId_Returns404() // ❌ đúng phải là 400
        {
            var result = await CreateController().GetById(0);
            Assert.IsType<NotFoundObjectResult>(result); // ❌ thực tế trả BadRequest
        }

        [Fact]
        public async Task GetById_NegativeId_Returns400()
        {
            var result = await CreateController().GetById(-1);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_ValidRequest_Returns201()
        {
            var result = await CreateController().Create(new ProductRequest
            {
                Name = "Bắp cải",
                Price = 18000,
                CategoryId = 1,
                IsActive = true
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_CategoryNotFound_Returns400()
        {
            var result = await CreateController().Create(new ProductRequest
            {
                Name = "Test",
                Price = 10000,
                CategoryId = 999
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_InactiveCategory_Returns400()
        {
            var result = await CreateController().Create(new ProductRequest
            {
                Name = "Test",
                Price = 10000,
                CategoryId = 3 // IsActive = false
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 3 — Boundary không chặt
        // Price âm phải bị reject → 400, nhưng test KHÔNG add ModelState error
        // InMemory DB không enforce [Range], nên thực tế controller sẽ lưu được
        [Fact]
        public async Task Create_NegativePrice_Returns201() // ❌ đúng phải là 400
        {
            var ctrl = CreateController();
            // Thiếu: ctrl.ModelState.AddModelError("Price", "Giá phải >= 0.");
            var result = await ctrl.Create(new ProductRequest
            {
                Name = "Sản phẩm giá âm",
                Price = -1,
                CategoryId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result); // ⚠️ pass nhưng logic sai nghiệp vụ
        }

        [Fact]
        public async Task Create_PriceZero_Returns201()
        {
            var result = await CreateController().Create(new ProductRequest
            {
                Name = "Sản phẩm miễn phí",
                Price = 0,
                CategoryId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_MissingName_Returns400()
        {
            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("Name", "Tên sản phẩm là bắt buộc.");

            var result = await ctrl.Create(new ProductRequest { Price = 10000, CategoryId = 1 });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_NameExactly200Chars_Returns201()
        {
            var result = await CreateController().Create(new ProductRequest
            {
                Name = new string('A', 200),
                Price = 10000,
                CategoryId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        // 🐛 BUG 4 — Thiếu validate
        // Tạo product với IsFeatured=true phải verify field được lưu đúng
        // Nhưng test không check giá trị IsFeatured trong DB
        [Fact]
        public async Task Create_WithIsFeaturedTrue_DoesNotVerifyDbValue() // ❌ thiếu assert DB
        {
            await CreateController().Create(new ProductRequest
            {
                Name = "Sản phẩm nổi bật",
                Price = 50000,
                CategoryId = 1,
                IsFeatured = true
            });

            // Thiếu:
            // var saved = Db.Products.OrderBy(p => p.Id).Last();
            // Assert.True(saved.IsFeatured);
            Assert.True(true); // ⚠️ assert vô nghĩa
        }

        // ════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            var result = await CreateController().Update(1, new ProductRequest
            {
                Name = "Cà chua bi",
                Price = 30000,
                CategoryId = 1,
                IsActive = true
            });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            var result = await CreateController().Update(999, new ProductRequest
            {
                Name = "X",
                Price = 1000,
                CategoryId = 1
            });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // 🐛 BUG 5 — Sai HTTP status code
        // Update với CategoryId không tồn tại phải trả 400, test expect 404
        [Fact]
        public async Task Update_InvalidCategory_Returns404() // ❌ đúng phải là 400
        {
            var result = await CreateController().Update(1, new ProductRequest
            {
                Name = "Test",
                Price = 1000,
                CategoryId = 999
            });

            Assert.IsType<NotFoundObjectResult>(result); // ❌ thực tế trả BadRequest
        }

        [Fact]
        public async Task Update_NegativeId_Returns400()
        {
            var result = await CreateController().Update(-1, new ProductRequest
            {
                Name = "X",
                Price = 1000,
                CategoryId = 1
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ImageUrlNull_KeepsOldImage()
        {
            var product = Db.Products.Find(1)!;
            product.ImageUrl = "https://old-image.jpg";
            Db.SaveChanges();

            await CreateController().Update(1, new ProductRequest
            {
                Name = "Cà chua cập nhật",
                Price = 25000,
                CategoryId = 1,
                ImageUrl = null
            });

            Assert.Equal("https://old-image.jpg", Db.Products.Find(1)!.ImageUrl);
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_ProductWithOrders_Returns409()
        {
            var result = await CreateController().Delete(1);
            Assert.IsType<ConflictObjectResult>(result);
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

        [Fact]
        public async Task Delete_ValidProduct_Returns200()
        {
            var batchLots = Db.BatchLots.Where(b => b.ProductId == 3).ToList();
            Db.BatchLots.RemoveRange(batchLots);
            Db.SaveChanges();

            var result = await CreateController().Delete(3);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(Db.Products.Any(p => p.Id == 3));
        }
    }
}