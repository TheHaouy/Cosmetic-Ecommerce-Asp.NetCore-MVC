using Microsoft.AspNetCore.Mvc;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class ChinhsachdoitraController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}