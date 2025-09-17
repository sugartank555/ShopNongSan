using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Services;
using ShopNongSan.ViewModels;

namespace ShopNongSan.Controllers
{
   [Authorize]
    public class CartController : Controller
    {
      
        private readonly ApplicationDbContext _db;
        private readonly ICartSession _cart;

        public CartController(ApplicationDbContext db, ICartSession cart)
        {
            _db = db;
            _cart = cart;
        }

        public IActionResult Index() => View(_cart.Get());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int qty = 1)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId);
            if (p == null) return NotFound();

            var cart = _cart.Get();
            var line = cart.Lines.FirstOrDefault(x => x.ProductId == productId);
            if (line == null)
            {
                cart.Lines.Add(new CartLineVM
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    Qty = qty
                });
            }
            else
            {
                line.Qty += qty;
            }

            _cart.Save(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int qty)
        {
            var cart = _cart.Get();
            var line = cart.Lines.FirstOrDefault(x => x.ProductId == productId);
            if (line != null)
            {
                line.Qty = Math.Max(1, qty);
                _cart.Save(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = _cart.Get();
            cart.Lines.RemoveAll(x => x.ProductId == productId);
            _cart.Save(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _cart.Clear();
            return RedirectToAction(nameof(Index));
        }
    }
}
