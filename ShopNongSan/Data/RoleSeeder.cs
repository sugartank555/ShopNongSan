using Microsoft.AspNetCore.Identity;

namespace ShopNongSan.Data
{
    public static class RoleSeeder
    {
        public static async Task Seed(IServiceProvider sp)
        {
            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = sp.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Admin", "Staff", "Customer" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            var email = "admin@shopnongsan.local";
            var admin = await userMgr.FindByEmailAsync(email);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await userMgr.CreateAsync(admin, "Admin@123"); // đổi sau khi chạy
                await userMgr.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
