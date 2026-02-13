using Microsoft.AspNetCore.Mvc;

namespace ShopNongSan.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
