using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly Cloudinary _cloudinary;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env, IConfiguration config)
        {
            _db = db;
            _env = env;

            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        // ===== HELPER: Upload ảnh lên Cloudinary =====
        private async Task<string?> UploadImageAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "shopnongsan/products"
            };
            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl?.ToString();
        }

        // ===== HELPER: Xóa ảnh trên Cloudinary =====
        private async Task DeleteImageAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');

                var startIndex = Array.IndexOf(segments, "shopnongsan");
                if (startIndex < 0) return;

                var publicIdParts = segments.Skip(startIndex).ToArray();
                publicIdParts[^1] = Path.GetFileNameWithoutExtension(publicIdParts[^1]);

                var publicId = string.Join("/", publicIdParts);
                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xóa ảnh Cloudinary: {ex.Message}");
            }
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var data = await _db.Products
                .Include(x => x.Category)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return View(data);
        }

        // ===== CREATE =====
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                return View(m);
            }

            if (imageFile != null && imageFile.Length > 0)
                m.ImageUrl = await UploadImageAsync(imageFile);

            _db.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm mới!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT =====
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Products.FindAsync(id);
            if (m == null) return NotFound();
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                return View(m);
            }

            var old = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == m.Id);
            if (old == null) return NotFound();

            if (imageFile != null && imageFile.Length > 0)
            {
                await DeleteImageAsync(old.ImageUrl);
                m.ImageUrl = await UploadImageAsync(imageFile);
            }
            else
            {
                m.ImageUrl = old.ImageUrl;
            }

            _db.Update(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật sản phẩm!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Products.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                TempData["Error"] = "Không thể xóa! Sản phẩm này đã có trong đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            var m = await _db.Products.FindAsync(id);
            if (m != null)
            {
                await DeleteImageAsync(m.ImageUrl);
                _db.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}