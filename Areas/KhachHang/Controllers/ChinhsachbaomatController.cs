using Microsoft.AspNetCore.Mvc;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class ChinhsachbaomatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
