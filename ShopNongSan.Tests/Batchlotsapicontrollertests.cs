using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Controllers.Api;
using ShopNongSan.Models;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class BatchLotsApiControllerTests : TestBase
    {
        private BatchLotsApiController CreateController()
        {
            var ctrl = new BatchLotsApiController(Db);
            SetAdminUser(ctrl);
            return ctrl;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAllBatchLots()
        {
            var result = await CreateController().GetAll(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        // 🐛 BUG 1 — Business logic sai
        // ProductId=1 chỉ có 1 lô (LOT-001), nhưng test expect 2
        [Fact]
        public async Task GetAll_FilterByProductId_Returns2() // ❌ đúng phải là 1
        {
            var result = await CreateController().GetAll(productId: 1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(2, list.Count()); // ❌ thực tế chỉ có 1 lô của product 1
        }

        [Fact]
        public async Task GetAll_FilterByNonExistProductId_ReturnsEmpty()
        {
            var result = await CreateController().GetAll(productId: 999);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Empty(list);
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

        // 🐛 BUG 2 — Sai HTTP status code
        // Id âm phải trả 400, test expect 404
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
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-NEW",
                FarmName = "Trang trại mới",
                HarvestDate = DateTime.Today,
                ExpireDate = DateTime.Today.AddMonths(3),
                QuantityIn = 200,
                ProductId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_DuplicateLotCode_Returns409()
        {
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-001",
                FarmName = "Trang trại khác",
                HarvestDate = DateTime.Today,
                QuantityIn = 50,
                ProductId = 1
            });

            Assert.IsType<ConflictObjectResult>(result);
        }

        // 🐛 BUG 3 — Boundary không chặt
        // ExpireDate = HarvestDate (bằng nhau) phải bị reject → 400
        // Nhưng test expect 201
        [Fact]
        public async Task Create_ExpireDateEqualsHarvestDate_Returns201() // ❌ đúng phải là 400
        {
            var today = DateTime.Today;
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-SAME",
                FarmName = "Test",
                HarvestDate = today,
                ExpireDate = today, // bằng nhau → không hợp lệ
                QuantityIn = 50,
                ProductId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result); // ❌ thực tế trả BadRequest
        }

        [Fact]
        public async Task Create_ExpireDateOneDayAfterHarvest_Returns201()
        {
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-BOUNDARY",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                ExpireDate = DateTime.Today.AddDays(1),
                QuantityIn = 50,
                ProductId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_ProductNotFound_Returns400()
        {
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-X",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                QuantityIn = 10,
                ProductId = 999
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_InactiveProduct_Returns400()
        {
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-INACT",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                QuantityIn = 10,
                ProductId = 3
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_QuantityInOne_Returns201()
        {
            var result = await CreateController().Create(new BatchLotRequest
            {
                LotCode = "LOT-QTY1",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                QuantityIn = 1,
                ProductId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Create_QuantityInZero_Returns400()
        {
            var ctrl = CreateController();
            ctrl.ModelState.AddModelError("QuantityIn", "Số lượng nhập kho phải >= 1.");

            var result = await ctrl.Create(new BatchLotRequest
            {
                LotCode = "LOT-QTY0",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                QuantityIn = 0,
                ProductId = 1
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            var result = await CreateController().Update(1, new BatchLotRequest
            {
                LotCode = "LOT-001-UPD",
                FarmName = "Trang trại A cập nhật",
                HarvestDate = DateTime.Today.AddDays(-10),
                ExpireDate = DateTime.Today.AddMonths(3),
                QuantityIn = 150,
                ProductId = 1
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // 🐛 BUG 4 — Thiếu validate
        // QuantityIn < QuantitySold phải trả 400
        // Nhưng test không check message lỗi cụ thể
        [Fact]
        public async Task Update_QuantityInLessThanSold_Returns400ButNoMessageCheck()
        {
            // LOT-001 QuantitySold = 20
            var result = await CreateController().Update(1, new BatchLotRequest
            {
                LotCode = "LOT-001",
                FarmName = "Test",
                HarvestDate = DateTime.Today.AddDays(-10),
                QuantityIn = 10, // < 20
                ProductId = 1
            });

            Assert.IsType<BadRequestObjectResult>(result);
            // Thiếu: Assert.Contains("QuantityIn", result.Value.ToString());
            // Bỏ sót kiểm tra nội dung message
        }

        [Fact]
        public async Task Update_QuantityInEqualSold_Returns200()
        {
            var result = await CreateController().Update(1, new BatchLotRequest
            {
                LotCode = "LOT-001",
                FarmName = "Test",
                HarvestDate = DateTime.Today.AddDays(-10),
                QuantityIn = 20, // = QuantitySold
                ProductId = 1
            });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            var result = await CreateController().Update(999, new BatchLotRequest
            {
                LotCode = "TEST",
                FarmName = "Test",
                HarvestDate = DateTime.Today,
                QuantityIn = 10,
                ProductId = 1
            });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_ValidBatchLot_Returns200()
        {
            var result = await CreateController().Delete(2);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(Db.BatchLots.Any(b => b.Id == 2));
        }

        [Fact]
        public async Task Delete_BatchLotWithOrderItems_Returns409()
        {
            Db.OrderItems.Add(new OrderItem
            {
                OrderId = 1,
                ProductId = 1,
                BatchLotId = 1,
                Quantity = 1,
                UnitPrice = 25000
            });
            Db.SaveChanges();

            var result = await CreateController().Delete(1);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            var result = await CreateController().Delete(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // 🐛 BUG 5 — Sai HTTP status code
        // Id âm phải trả 400, test expect 404
        [Fact]
        public async Task Delete_NegativeId_Returns404() // ❌ đúng phải là 400
        {
            var result = await CreateController().Delete(-1);
            Assert.IsType<NotFoundObjectResult>(result); // ❌ thực tế trả BadRequest
        }
    }
}