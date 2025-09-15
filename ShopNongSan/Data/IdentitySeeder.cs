// Data/IdentitySeeder.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Models;

namespace ShopNongSan.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1) Seed Roles
            string[] roles = ["Admin", "Customer"];
            foreach (var r in roles)
            {
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));
            }

            // 2) Seed tài khoản Admin (nếu chưa có)
            var adminEmail = "admin@nongsan.local";
            var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Quản trị",
                    Address = "TP.HCM"
                };

                // Lưu ý: đổi mật khẩu khi triển khai thực tế
                var createResult = await userMgr.CreateAsync(admin, "Admin@123");
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(
                        "Không tạo được tài khoản Admin: " +
                        string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"))
                    );
            }

            // Đảm bảo Admin có role "Admin"
            if (!await userMgr.IsInRoleAsync(admin, "Admin"))
                await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}
