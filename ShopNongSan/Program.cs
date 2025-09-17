using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;
using ShopNongSan.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing DefaultConnection.");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(conn));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Cart session service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ShopNongSan.Services.ICartSession, ShopNongSan.Services.CartSession>();

// Identity (đăng ký MỘT LẦN)
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

// Cookie paths
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.LogoutPath = "/Identity/Account/Logout";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Session
builder.Services.AddDistributedMemoryCache();   // <— BẮT BUỘC
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<ICartSession, CartSession>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // <— đặt trước auth
app.UseAuthentication();
app.UseAuthorization();

// routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
