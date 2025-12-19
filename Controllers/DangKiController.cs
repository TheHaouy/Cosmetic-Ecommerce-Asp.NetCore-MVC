using Microsoft.AspNetCore.Mvc;
using Final_VS1.Models;
using Final_VS1.Data;
using System;
using System.Threading.Tasks;
using System.Linq;
using Final_VS1.Helper;

namespace Final_VS1.Controllers
{
    public class DangKiController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly IEmailSender _emailSender;

        public DangKiController(LittleFishBeautyContext context, IEmailSender emailSender)
        {
            _emailSender = emailSender;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        #region Đăng Kí tài Khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DangKiViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                if (_context.TaiKhoans.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View("Index", model);
                }

                // Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    Email = model.Email,
                    MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau),
                    HoTen = model.HoTen,
                    VaiTro = "Khach",
                    TrangThai = false, // Chưa xác nhận email
                    NgayTao = DateTime.Now
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();
            #region Xác nhận email
                // Tạo token xác nhận
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var confirmationLink = Url.Action("XacNhanEmail", "DangKi",
                    new { email = model.Email, token = token }, Request.Scheme);

                // Lưu token vào session
                var sessionKey = $"EmailConfirmation_{model.Email}";
                HttpContext.Session.SetString(sessionKey, token);
                HttpContext.Session.SetString($"{sessionKey}_CreatedAt", DateTime.Now.ToString());

                try
                {
                    // Gửi email xác nhận
                    await _emailSender.SenderEmailAsync(model.Email, "Xác nhận tài khoản",
                $@"
                
<!DOCTYPE html>
<html>
<head>
    <link href='https://fonts.googleapis.com/css2?family=Roboto:wght@400&display=swap' rel='stylesheet'>
</head>
<body>
    <div style='font-family: Roboto, Arial, sans-serif; max-width: 650px; margin: 0 auto; background: #f0f0f0; padding: 20px; border-radius: 10px;'>
        
        <!-- Header Section -->
        <div style='background: white; border-radius: 10px; padding: 20px; box-shadow: 0 5px 15px rgba(0,0,0,0.1); text-align: center;'>
            <h1 style='font-size: 36px; color: #4CAF50; margin: 0;'>Little Fish Beauty</h1>
            <h2 style='font-size: 24px; color: #333; margin: 10px 0;'>🎉 Kích hoạt tài khoản 🎉</h2>
        </div>

        <!-- Main Content -->
        <div style='color: #555; line-height: 1.6; font-size: 16px; padding: 20px;'>
            <p>Xin chào <strong style='color: #4CAF50;'>{model.HoTen}</strong>!</p>
            <p>Cảm ơn bạn đã chọn <strong style='color: #4CAF50;'>Little Fish Beauty</strong>. Để hoàn tất đăng ký, vui lòng kích hoạt tài khoản của bạn:</p>
        </div>

        <!-- Activation Button -->
        <div style='text-align: center; margin: 20px 0;'>
            <a href='{confirmationLink}' 
            style='display: inline-block; background: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: 500; font-size: 16px; transition: background 0.3s ease;'>
                Kích hoạt tài khoản
            </a>
        </div>

        <!-- Security Note -->
        <div style='background: #f8f9fa; border-radius: 5px; padding: 15px; margin: 20px 0;'>
            <p style='color: #6c757d; font-size: 13px; margin: 0; text-align: center;'>
                🔒 Để bảo mật tài khoản, vui lòng không chia sẻ email này với người khác.
            </p>
        </div>

        <!-- Thank You Message -->
        <div style='text-align: center; margin-top: 20px;'>
            <p style='color: #4CAF50; font-size: 16px; margin: 0;'>Cảm ơn bạn đã chọn Little Fish Beauty! 🌟</p>
        </div>

        <!-- Footer -->
        <div style='text-align: center; margin-top: 20px;'>
            <p style='color: rgba(0,0,0,0.6); font-size: 12px; margin: 0;'>© 2025 Little Fish Beauty. All rights reserved.</p>
        </div>

    </div>
</body>
</html>");
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                }
                catch (Exception ex)
                {
                    // Log lỗi và thông báo cho người dùng
                    TempData["ErrorMessage"] = "Đăng ký thành công nhưng không thể gửi email xác nhận. Vui lòng liên hệ admin.";
                    // Log: ex.Message để debug
                }

                return RedirectToAction("Index");
            }

            // Nếu ModelState không hợp lệ, quay lại form với thông báo lỗi
            return View("Index", model);
        }
        #endregion
            #region Kiểm tra Email
        [HttpPost]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var exists = await Task.FromResult(_context.TaiKhoans.Any(u => u.Email == email));
            return Json(!exists); // Trả về true nếu email chưa tồn tại
        }

        //Action xác nhận email
        [HttpGet]
        public async Task<IActionResult> XacNhanEmail(string email, string token)
        {
            var sessionKey = $"EmailConfirmation_{email}";
            var storedToken = HttpContext.Session.GetString(sessionKey);
            var createdAtString = HttpContext.Session.GetString($"{sessionKey}_CreatedAt");

            // Kiểm tra token và thời gian hết hạn
            if (string.IsNullOrEmpty(storedToken) || storedToken != token)
            {
                TempData["ErrorMessage"] = "Xác nhận thất bại. Liên kết không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Index");
            }

            // Kiểm tra thời gian hết hạn (24 giờ)
            if (!string.IsNullOrEmpty(createdAtString) && DateTime.TryParse(createdAtString, out var createdAt))
            {
                if (DateTime.Now.Subtract(createdAt).TotalHours > 24)
                {
                    HttpContext.Session.Remove(sessionKey);
                    HttpContext.Session.Remove($"{sessionKey}_CreatedAt");
                    TempData["ErrorMessage"] = "Liên kết xác nhận đã hết hạn. Vui lòng đăng ký lại.";
                    return RedirectToAction("Index");
                }
            }

            // Tìm user và cập nhật trạng thái
            var user = _context.TaiKhoans.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Xác nhận thất bại. Tài khoản không tồn tại.";
                return RedirectToAction("Index");
            }

            user.TrangThai = true;
            await _context.SaveChangesAsync();

            // Xóa token khỏi session
            HttpContext.Session.Remove(sessionKey);
            HttpContext.Session.Remove($"{sessionKey}_CreatedAt");

            TempData["Success"] = "Tài khoản của bạn đã được kích hoạt thành công! Bạn có thể đăng nhập.";
            return RedirectToAction("Index", "DangNhap");
        }
    }
}
#endregion
#endregion
