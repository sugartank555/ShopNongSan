using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        public CustomersController(UserManager<IdentityUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        // 🟢 Danh sách người dùng với tìm kiếm, lọc, phân trang
        public async Task<IActionResult> Index(string? search, string? role, int page = 1, int pageSize = 8)
        {
            var users = _userMgr.Users.ToList();
            var model = new List<CustomerViewModel>();

            foreach (var user in users)
            {
                var roles = await _userMgr.GetRolesAsync(user);
                model.Add(new CustomerViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "Chưa có",
                    UserName = user.UserName ?? user.Email ?? "N/A",
                    Roles = string.Join(", ", roles),
                    IsEmailConfirmed = user.EmailConfirmed,
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now
                });
            }

            // 🔍 Lọc theo role
            if (!string.IsNullOrEmpty(role) && role != "Tất cả")
                model = model.Where(u => u.Roles.Contains(role)).ToList();

            // 🔎 Tìm kiếm
            if (!string.IsNullOrEmpty(search))
                model = model.Where(u =>
                    u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            // 📄 Phân trang
            int total = model.Count;
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            model = model.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.RoleFilter = role;
            ViewBag.Roles = _roleMgr.Roles.Select(r => r.Name).ToList();

            return View(model);
        }

        // 🟡 Khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userMgr.UpdateAsync(user);

            TempData["Success"] = $"Đã khóa tài khoản: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // 🟢 Mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            await _userMgr.UpdateAsync(user);

            TempData["Success"] = $"Đã mở khóa tài khoản: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // ⚙️ Gán quyền (Admin có thể thay đổi Role người dùng)
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string id, string newRole)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();

            var oldRoles = await _userMgr.GetRolesAsync(user);
            await _userMgr.RemoveFromRolesAsync(user, oldRoles);
            await _userMgr.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Đã gán quyền '{newRole}' cho {user.Email}";
            return RedirectToAction(nameof(Index));
        }
    }

    // 🧩 ViewModel hiển thị
    public class CustomerViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }
        public bool IsLocked { get; set; }
    }
}
