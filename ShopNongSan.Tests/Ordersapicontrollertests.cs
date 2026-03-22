using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Controllers.Api;
using ShopNongSan.Models;
using Xunit;

namespace ShopNongSan.Tests.Controllers
{
    public class OrdersApiControllerTests : TestBase
    {
        private OrdersApiController CreateController()
        {
            var ctrl = new OrdersApiController(Db);
            SetAdminUser(ctrl);
            return ctrl;
        }

        // ════════════════════════════════════════════════════════
        // GET ALL
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAllOrders()
        {
            var result = await CreateController().GetAll(null, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public async Task GetAll_FilterByPending_Returns1()
        {
            var result = await CreateController().GetAll(OrderStatus.Pending, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list);
        }

        // 🐛 BUG 1 — Business logic sai
        // Filter Cancelled không có order nào → phải trả empty
        // Nhưng test expect 1
        [Fact]
        public async Task GetAll_FilterByCancelled_Returns1() // ❌ đúng phải là empty
        {
            var result = await CreateController().GetAll(OrderStatus.Cancelled, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list); // ❌ thực tế không có order nào bị Cancelled
        }

        [Fact]
        public async Task GetAll_FilterByNonExistStatus_ReturnsEmpty()
        {
            var result = await CreateController().GetAll(OrderStatus.Refunded, null);

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

        // 🐛 BUG 2 — Boundary không chặt
        // Id = 0 phải trả 400, test expect 404
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

        // 🐛 BUG 3 — Thiếu validate
        // GetById trả về detail có Items, nhưng test không kiểm tra Items
        [Fact]
        public async Task GetById_Order1_DoesNotCheckItems() // ❌ thiếu assert Items
        {
            var result = await CreateController().GetById(1);

            Assert.IsType<OkObjectResult>(result);
            // Thiếu:
            // var detail = (OrderDetailResponse)((OkObjectResult)result).Value!;
            // Assert.NotEmpty(detail.Items);
            // Assert.Equal(1, detail.Items.Count);
        }

        // ════════════════════════════════════════════════════════
        // UPDATE STATUS
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateStatus_PendingToPaid_Returns200()
        {
            var result = await CreateController()
                .UpdateStatus(1, new UpdateOrderStatusRequest { Status = OrderStatus.Paid });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_PendingToPaid_SetsPaidAt()
        {
            await CreateController()
                .UpdateStatus(1, new UpdateOrderStatusRequest { Status = OrderStatus.Paid });

            Assert.NotNull(Db.Orders.Find(1)!.PaidAt);
        }

        // 🐛 BUG 4 — Business logic sai
        // Đổi trạng thái Completed → Pending phải bị chặn → 422
        // Nhưng test expect 200
        [Fact]
        public async Task UpdateStatus_CompletedToPending_Returns200() // ❌ đúng phải là 422
        {
            var result = await CreateController()
                .UpdateStatus(3, new UpdateOrderStatusRequest { Status = OrderStatus.Pending });

            Assert.IsType<OkObjectResult>(result); // ❌ thực tế trả UnprocessableEntity
        }

        [Fact]
        public async Task UpdateStatus_NotFound_Returns404()
        {
            var result = await CreateController()
                .UpdateStatus(999, new UpdateOrderStatusRequest { Status = OrderStatus.Paid });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_NegativeId_Returns400()
        {
            var result = await CreateController()
                .UpdateStatus(-1, new UpdateOrderStatusRequest { Status = OrderStatus.Paid });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(OrderStatus.Completed)]
        [InlineData(OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Refunded)]
        public async Task UpdateStatus_TerminalStatuses_Returns422(OrderStatus terminal)
        {
            var order = Db.Orders.Find(1)!;
            order.Status = terminal;
            Db.SaveChanges();

            var result = await CreateController()
                .UpdateStatus(1, new UpdateOrderStatusRequest { Status = OrderStatus.Pending });

            Assert.IsType<UnprocessableEntityObjectResult>(result);
        }

        // 🐛 BUG 5 — Sai HTTP status code
        // Đơn Paid → cập nhật trạng thái hợp lệ → 200
        // Nhưng test expect 422 (nhầm với terminal status)
        [Fact]
        public async Task UpdateStatus_PaidToShipped_Returns422() // ❌ đúng phải là 200
        {
            // Order 2 đang Paid → Shipped là hợp lệ
            var result = await CreateController()
                .UpdateStatus(2, new UpdateOrderStatusRequest { Status = OrderStatus.Shipped });

            Assert.IsType<UnprocessableEntityObjectResult>(result); // ❌ thực tế trả 200
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════

        [Fact]
        public async Task Delete_PendingOrder_Returns200()
        {
            var result = await CreateController().Delete(1);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(Db.Orders.Any(o => o.Id == 1));
        }

        [Fact]
        public async Task Delete_PaidOrder_Returns422()
        {
            var result = await CreateController().Delete(2);
            Assert.IsType<UnprocessableEntityObjectResult>(result);
        }

        [Fact]
        public async Task Delete_CompletedOrder_Returns422()
        {
            var result = await CreateController().Delete(3);
            Assert.IsType<UnprocessableEntityObjectResult>(result);
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
        public async Task Delete_PendingOrder_CascadeDeletesOrderItems()
        {
            await CreateController().Delete(1);
            Assert.False(Db.OrderItems.Any(oi => oi.OrderId == 1));
        }
    }
}