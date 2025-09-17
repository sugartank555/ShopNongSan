using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;
using System.IO; // <- để dùng Path, FileStream
using System.Text.RegularExpressions;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly long _maxImageSize = 2 * 1024 * 1024; // 2MB
        private static readonly string[] _allowedImageTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };

        public ProductsController(ApplicationDbContext db) => _db = db;

        // ================== INDEX ==================
        public async Task<IActionResult> Index(string? q, int? categoryId)
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", categoryId
            );

            var query = _db.Products.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q));

            return View(await query.OrderByDescending(p => p.Id).ToListAsync());
        }

        // ================== CREATE ==================
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name"
            );

            return View(new Product { Stock = 10, Price = 10000, IsOrganic = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? ImageFile)
        {
            // Gợi ý slug nếu người dùng để trống
            if (string.IsNullOrWhiteSpace(model.Slug) && !string.IsNullOrWhiteSpace(model.Name))
                model.Slug = ToSlug(model.Name);

            if (!ModelState.IsValid)
            {
                AddAllModelErrorsToSummary();
                await LoadCategories(model.CategoryId);
                return View(model);
            }

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var saveResult = await SaveImageAsync(ImageFile);
                    if (!saveResult.ok)
                    {
                        ModelState.AddModelError(nameof(model.ImageUrl), saveResult.error!);
                        await LoadCategories(model.CategoryId);
                        return View(model);
                    }
                    model.ImageUrl = saveResult.webPath!;
                }

                _db.Products.Add(model);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Tạo sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lưu CSDL: " + ex.Message);
                await LoadCategories(model.CategoryId);
                return View(model);
            }
        }

        // ================== EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();

            await LoadCategories(p.CategoryId);
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? ImageFile)
        {
            // Gợi ý slug nếu trống
            if (string.IsNullOrWhiteSpace(model.Slug) && !string.IsNullOrWhiteSpace(model.Name))
                model.Slug = ToSlug(model.Name);

            if (!ModelState.IsValid)
            {
                AddAllModelErrorsToSummary();
                await LoadCategories(model.CategoryId);
                return View(model);
            }

            var existing = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Id);
            if (existing == null) return NotFound();

            try
            {
                // Nếu có upload ảnh mới
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var saveResult = await SaveImageAsync(ImageFile);
                    if (!saveResult.ok)
                    {
                        ModelState.AddModelError(nameof(model.ImageUrl), saveResult.error!);
                        await LoadCategories(model.CategoryId);
                        return View(model);
                    }

                    // Xoá ảnh cũ (nếu có) và khác ảnh mới
                    if (!string.IsNullOrEmpty(existing.ImageUrl))
                        DeleteImage(existing.ImageUrl);

                    model.ImageUrl = saveResult.webPath!;
                }
                else
                {
                    // Giữ nguyên ảnh cũ nếu không upload mới
                    model.ImageUrl = existing.ImageUrl;
                }

                _db.Update(model);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Cập nhật sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi cập nhật: " + ex.Message);
                await LoadCategories(model.CategoryId);
                return View(model);
            }
        }

        // ================== DELETE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null)
            {
                if (!string.IsNullOrEmpty(p.ImageUrl)) DeleteImage(p.ImageUrl);
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã xoá sản phẩm.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ================== Helpers ==================
        private async Task LoadCategories(int? selectedId = null)
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selectedId
            );
        }

        private void AddAllModelErrorsToSummary()
        {
            var all = string.Join(" | ", ModelState
                .Where(kv => kv.Value!.Errors.Count > 0)
                .Select(kv => $"{kv.Key}: {string.Join(",", kv.Value!.Errors.Select(e => e.ErrorMessage))}"));
            if (!string.IsNullOrEmpty(all))
                ModelState.AddModelError(string.Empty, "Lỗi nhập liệu: " + all);
        }

        private async Task<(bool ok, string? webPath, string? error)> SaveImageAsync(IFormFile file)
        {
            if (!_allowedImageTypes.Contains(file.ContentType))
                return (false, null, "Định dạng ảnh không hợp lệ (chỉ nhận JPG/PNG/WebP/GIF)");

            if (file.Length > _maxImageSize)
                return (false, null, $"Ảnh vượt quá {_maxImageSize / (1024 * 1024)}MB");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName);
            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            // Làm sạch tên file
            safeName = Regex.Replace(safeName, @"[^a-zA-Z0-9_-]+", "-").Trim('-');
            var newFileName = $"{safeName}-{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, newFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // web path để gán vào src
            return (true, "/uploads/" + newFileName, null);
        }

        private void DeleteImage(string webPath)
        {
            // webPath như: /uploads/abc.jpg
            if (string.IsNullOrWhiteSpace(webPath)) return;
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physical = Path.Combine(root, webPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(physical))
            {
                try { System.IO.File.Delete(physical); } catch { /* ignore */ }
            }
        }

        private static string ToSlug(string input)
        {
            // Slug đơn giản (bạn có thể thay bằng utility chuẩn hơn nếu muốn)
            string s = input.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^a-z0-9-]", "");
            s = Regex.Replace(s, @"-+", "-").Trim('-');
            return s;
        }
    }
}
