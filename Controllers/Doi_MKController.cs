using Microsoft.AspNetCore.Mvc;
using Final_VS1.Data;
using Final_VS1.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Final_VS1.Controllers
{
    public class Doi_MKController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public Doi_MKController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string email, string code)
        {
            var model = new DoiMKViewModel
            {
                Email = email,
                Code = code
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DoiMKViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra mã xác nhận từ session
                var sessionCode = HttpContext.Session.GetString($"ResetCode_{model.Email}");
                var sessionExpire = HttpContext.Session.GetString($"ResetCodeExpire_{model.Email}");

                if (string.IsNullOrEmpty(sessionCode) || string.IsNullOrEmpty(sessionExpire))
                {
                    TempData["Error"] = "Mã xác nhận không hợp lệ hoặc đã hết hạn";
                    return View(model);
                }

                // Kiểm tra thời gian hết hạn
                if (DateTime.TryParse(sessionExpire, out DateTime expireTime) && DateTime.Now > expireTime)
                {
                    TempData["Error"] = "Mã xác nhận đã hết hạn. Vui lòng yêu cầu mã mới!";
                    return View(model);
                }

                // Kiểm tra mã xác nhận
                if (sessionCode != model.Code)
                {
                    TempData["Error"] = "Mã xác nhận không đúng";
                    return View(model);
                }

                // Tìm user và cập nhật mật khẩu
                var user = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy tài khoản";
                    return View(model);
                }

                // Mã hóa mật khẩu mới
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                
                await _context.SaveChangesAsync();

                // Xóa session
                HttpContext.Session.Remove($"ResetCode_{model.Email}");
                HttpContext.Session.Remove($"ResetCodeExpire_{model.Email}");
                HttpContext.Session.Remove("ResetEmail");

                TempData["Success"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Index", "DangNhap");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại!";
                return View(model);
            }
        }
    }
}
