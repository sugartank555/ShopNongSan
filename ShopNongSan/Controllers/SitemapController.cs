using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using System.Text;

namespace ShopNongSan.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SitemapController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var baseUrl = "https://shopnongsan.id.vn";

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // ✅ Trang tĩnh
            var staticPages = new[]
            {
                ("/",              "1.0", "daily"),
                ("/Shop",          "0.9", "daily"),
                ("/Shop/Index",    "0.9", "daily"),
            };

            foreach (var (url, priority, freq) in staticPages)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{url}</loc>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>{freq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                sb.AppendLine("  </url>");
            }

            // ✅ Sản phẩm động — dùng đúng field CreatedAt từ model Product
            var products = await _db.Products
                .Where(p => p.IsActive)
                .Select(p => new { p.Id, p.CreatedAt })
                .ToListAsync();

            foreach (var p in products)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/Shop/Details/{p.Id}</loc>");
                sb.AppendLine($"    <lastmod>{p.CreatedAt:yyyy-MM-dd}</lastmod>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.7</priority>");
                sb.AppendLine("  </url>");
            }

            // ✅ Danh mục động
            var categories = await _db.Categories
                .Select(c => new { c.Id })
                .ToListAsync();

            foreach (var c in categories)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}/Shop?categoryId={c.Id}</loc>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.6</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}