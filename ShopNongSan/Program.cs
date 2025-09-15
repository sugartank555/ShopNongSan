using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(conn));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2) Identity (ĐĂNG KÝ MỘT LẦN)
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequiredLength = 6;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Password.RequireUppercase = false;
        opts.Password.RequireLowercase = false;
        opts.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// (tuỳ chọn) Đặt đường dẫn login/logout nếu bạn muốn
// builder.Services.ConfigureApplicationCookie(o =>
// {
//     o.LoginPath = "/Identity/Account/Login";
//     o.LogoutPath = "/Identity/Account/Logout";
//     o.AccessDeniedPath = "/Identity/Account/AccessDenied";
// });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 3) Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint(); // trang lỗi migration đẹp hơn
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 4) Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Bắt buộc để Identity Razor Pages hoạt động: /Identity/Account/*
app.MapRazorPages();

app.Run();
