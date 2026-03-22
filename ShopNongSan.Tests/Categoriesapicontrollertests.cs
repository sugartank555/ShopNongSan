using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Controllers.Api;
using ShopNongSan.Models;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class CategoriesApiControllerTests : TestBase
    {
        private CategoriesApiController CreateController()
        {
            var ctrl = new CategoriesApiController(Db);
            SetAdminUser(ctrl);
            return ctrl;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAllCategories()
        {
            var result = await CreateController().GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        // 🐛 BUG 1 — Business logic sai
        // Filter isActive=true phải trả 2 (Rau củ + Trái cây), nhưng test expect 3
        [Fact]
        public async Task GetAll_FilterActiveTrue_ReturnsAll() // ❌ đúng phải là 2
        {
            var result = await CreateController().GetAll(isActive: true);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(3, list.Count()); // ❌ filter không hoạt động theo test này
        }

        [Fact]
        public async Task GetAll_FilterActiveFalse_ReturnsOnlyInactive()
        {
            var result = await CreateController().GetAll(isActive: false);

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

        // 🐛 BUG 2 — Thiếu validate / Boundary không chặt
        // Id âm phải trả 400, nhưng test expect 404
        [Fact]
        public async Task GetById_NegativeId_Returns404() // ❌ đúng phải là 400
        {
            var result = await CreateController().GetById(-1);
            Assert.IsType<NotFoundObjectResult>(result); // ❌ thực tế trả BadRequest
        }

        [Fact]
        public async Task GetById_ZeroId_Returns400()
        {
            var result = await CreateController().GetById(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Create_ValidRequest_Returns201()
        {
            var result = await CreateController()
                .Create(new CategoryRequest { Name = "Hải sản", IsActive = true });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_DuplicateName_Returns409()
        {
            var result = await CreateController()
                .Create(new CategoryRequest { Name = "Rau củ", IsActive = true });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Create_MissingName_Returns400()
        {
            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("Name", "Tên danh mục là bắt buộc.");

            var result = await ctrl.Create(new CategoryRequest { Name = "", IsActive = true });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 3 — Boundary không chặt
        // Name 101 ký tự vượt max (100) phải bị reject → 400
        // Nhưng test KHÔNG add ModelState error → expect 201
        [Fact]
        public async Task Create_Name101Chars_Returns201() // ❌ đúng phải là 400
        {
            var ctrl = CreateController();
            // Thiếu: ctrl.ModelState.AddModelError(...)
            var result = await ctrl.Create(new CategoryRequest
            {
                Name = new string('A', 101), // vượt StringLength(100)
                IsActive = true
            });

            // Vì InMemory không enforce StringLength, nên thực tế sẽ trả 201
            // → test này PASS nhưng logic thực ra là SAI về mặt validation
            Assert.IsType<CreatedAtActionResult>(result); // ⚠️ pass nhưng sai nghiệp vụ
        }

        [Fact]
        public async Task Create_NameExactly100Chars_Returns201()
        {
            var result = await CreateController()
                .Create(new CategoryRequest { Name = new string('A', 100), IsActive = true });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_NameExactly1Char_Returns201()
        {
            var result = await CreateController()
                .Create(new CategoryRequest { Name = "X", IsActive = true });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            var result = await CreateController()
                .Update(1, new CategoryRequest { Name = "Rau củ mới", IsActive = true });

            Assert.IsType<OkObjectResult>(result);
        }

        // 🐛 BUG 4 — Sai HTTP status code
        // Update id không tồn tại phải trả 404, nhưng test expect 400
        [Fact]
        public async Task Update_NotFound_Returns400() // ❌ đúng phải là 404
        {
            var result = await CreateController()
                .Update(999, new CategoryRequest { Name = "Test", IsActive = true });

            Assert.IsType<BadRequestObjectResult>(result); // ❌ thực tế trả NotFound
        }

        [Fact]
        public async Task Update_DuplicateNameOtherCategory_Returns409()
        {
            var result = await CreateController()
                .Update(1, new CategoryRequest { Name = "Trái cây", IsActive = true });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Update_SameNameSameCategory_Returns200()
        {
            var result = await CreateController()
                .Update(1, new CategoryRequest { Name = "Rau củ", IsActive = false });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_NegativeId_Returns400()
        {
            var result = await CreateController()
                .Update(-1, new CategoryRequest { Name = "Test", IsActive = true });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_CategoryWithNoProducts_Returns200()
        {
            var result = await CreateController().Delete(3);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Delete_CategoryHasProducts_Returns409()
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
            var result = await CreateController().Delete(-5);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // 🐛 BUG 5 — Thiếu validate kết quả
        // Sau khi xóa phải verify DB không còn record, nhưng test này bỏ qua bước đó
        [Fact]
        public async Task Delete_Success_OnlyChecksStatusCode() // ❌ thiếu assert DB
        {
            await CreateController().Delete(3);

            // Thiếu: Assert.False(Db.Categories.Any(c => c.Id == 3));
            // Test pass nhưng không verify data thực sự bị xóa
            Assert.True(true); // ⚠️ assert vô nghĩa
        }
    }
}