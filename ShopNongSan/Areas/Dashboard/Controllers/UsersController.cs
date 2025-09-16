using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        public UsersController(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var users = _userMgr.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                users = users.Where(u =>
                    (u.Email != null && u.Email.Contains(q)) ||
                    (u.UserName != null && u.UserName.Contains(q)) ||
                    (u.FullName != null && u.FullName.Contains(q)));

            var list = await users.OrderBy(u => u.Email).ToListAsync();

            var map = new Dictionary<string, IList<string>>();
            foreach (var u in list)
                map[u.Id] = await _userMgr.GetRolesAsync(u);

            ViewBag.Roles = _roleMgr.Roles.Select(r => r.Name).ToList();
            ViewBag.UserRoles = map;
            ViewBag.Query = q;
            return View(list); // Views/Dashboard/Users/Index.cshtml
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToRole(string userId, string role)
        {
            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleMgr.RoleExistsAsync(role))
                await _roleMgr.CreateAsync(new IdentityRole(role));

            if (!await _userMgr.IsInRoleAsync(user, role))
                await _userMgr.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromRole(string userId, string role)
        {
            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userMgr.IsInRoleAsync(user, role))
                await _userMgr.RemoveFromRoleAsync(user, role);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string userId, int days = 30)
        {
            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userMgr.SetLockoutEnabledAsync(user, true);
            await _userMgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(days));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string userId)
        {
            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userMgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userMgr.FindByIdAsync(userId);
            if (user != null)
                await _userMgr.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
