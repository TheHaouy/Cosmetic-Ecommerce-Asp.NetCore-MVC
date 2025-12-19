using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Final_VS1.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Areas.KhachHang.ViewModels;
using System.IO;
using System.Threading.Tasks;
using BCrypt.Net;
using Final_VS1.Services;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class ThongTinController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly IMailchimpService _mailchimpService;

        public ThongTinController(LittleFishBeautyContext context, IMailchimpService mailchimpService)
        {
            _context = context;
            _mailchimpService = mailchimpService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var user = await _context.TaiKhoans
                .Include(t => t.DonHangs)
                .FirstOrDefaultAsync(t => t.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] string HoTen, [FromForm] string GioiTinh, [FromForm] IFormFile AnhDaiDien, [FromForm] bool NhanEmailMarketing)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });

                var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email == userEmail);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });

                // Validate tên
                if (string.IsNullOrWhiteSpace(HoTen))
                    return Json(new { success = false, message = "Họ và tên không được để trống" });

                Console.WriteLine($"=== DEBUG UpdateProfile ===");
                Console.WriteLine($"Received HoTen: '{HoTen}'");
                Console.WriteLine($"Received GioiTinh: '{GioiTinh}'");
                Console.WriteLine($"Received NhanEmailMarketing: {NhanEmailMarketing}");
                Console.WriteLine($"AnhDaiDien file: {(AnhDaiDien != null ? $"{AnhDaiDien.FileName} ({AnhDaiDien.Length} bytes)" : "null")}");
                Console.WriteLine($"User before update - HoTen: '{user.HoTen}', GioiTinh: '{user.GioiTinh}', NhanEmailMarketing: {user.NhanEmailMarketing}");

                // Xử lý Mailchimp subscribe/unsubscribe
                var previousEmailMarketingStatus = user.NhanEmailMarketing ?? false;
                if (NhanEmailMarketing != previousEmailMarketingStatus)
                {
                    if (NhanEmailMarketing)
                    {
                        // Đăng ký nhận email marketing
                        var subscribedSuccess = await _mailchimpService.SubscribeAsync(user.Email, user.HoTen ?? "", "");
                        if (subscribedSuccess)
                        {
                            Console.WriteLine($"Đã đăng ký email {user.Email} vào Mailchimp");
                        }
                        else
                        {
                            Console.WriteLine($"Không thể đăng ký email {user.Email} vào Mailchimp");
                        }
                    }
                    else
                    {
                        // Hủy đăng ký email marketing
                        var unsubscribedSuccess = await _mailchimpService.UnsubscribeAsync(user.Email);
                        if (unsubscribedSuccess)
                        {
                            Console.WriteLine($"Đã hủy đăng ký email {user.Email} khỏi Mailchimp");
                        }
                        else
                        {
                            Console.WriteLine($"Không thể hủy đăng ký email {user.Email} khỏi Mailchimp");
                        }
                    }
                }

                // Cập nhật thông tin cơ bản
                user.HoTen = HoTen.Trim();
                user.GioiTinh = string.IsNullOrEmpty(GioiTinh) ? null : GioiTinh.Trim();
                user.NhanEmailMarketing = NhanEmailMarketing;

                Console.WriteLine($"User after update - HoTen: '{user.HoTen}', GioiTinh: '{user.GioiTinh}', NhanEmailMarketing: {user.NhanEmailMarketing}");

                // Xử lý ảnh đại diện nếu có
                string newAvatarPath = null;
                if (AnhDaiDien != null && AnhDaiDien.Length > 0)
                {
                    Console.WriteLine("Processing avatar upload...");

                    // Validate file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(AnhDaiDien.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                        return Json(new { success = false, message = "Chỉ nhận file ảnh JPG, JPEG, PNG, GIF" });

                    if (AnhDaiDien.Length > 10 * 1024 * 1024) // 10MB
                        return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 10MB" });

                    // Tạo tên file unique
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Avatars");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(imagesFolder))
                    {
                        Directory.CreateDirectory(imagesFolder);
                        Console.WriteLine("Created Avatars directory");
                    }

                    var savePath = Path.Combine(imagesFolder, fileName);

                    // Lưu file
                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await AnhDaiDien.CopyToAsync(stream);
                    }

                    newAvatarPath = $"/Images/Avatars/{fileName}";

                    // Xóa ảnh cũ nếu có và khác ảnh mặc định
                    if (!string.IsNullOrEmpty(user.AnhDaiDien) && !user.AnhDaiDien.Contains("default"))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AnhDaiDien.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                                Console.WriteLine("Deleted old avatar");
                            }
                            catch (Exception deleteEx)
                            {
                                Console.WriteLine($"Could not delete old avatar: {deleteEx.Message}");
                            }
                        }
                    }

                    user.AnhDaiDien = newAvatarPath;
                    Console.WriteLine($"New avatar path: {newAvatarPath}");
                }

                // Lưu vào database
                _context.TaiKhoans.Update(user);
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"Database changes saved: {changes}");

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin thành công!",
                    anhDaiDien = newAvatarPath ?? user.AnhDaiDien,
                    hoTen = user.HoTen,
                    gioiTinh = user.GioiTinh
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR ===");
                Console.WriteLine($"Error message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCurrentPassword(string currentPassword)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });

                var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email == userEmail);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });

                if (string.IsNullOrWhiteSpace(currentPassword))
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu" });

                // Kiểm tra mật khẩu hiện tại
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(currentPassword, user.MatKhau);
                
                return Json(new { success = isPasswordCorrect });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying password: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });

            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email == userEmail);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });

            // Validate input
            if (string.IsNullOrWhiteSpace(currentPassword))
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại" });

            if (string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu mới" });

            if (newPassword.Length < 8)
                return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 8 ký tự" });

            // Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.MatKhau))
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });

            // Kiểm tra mật khẩu mới không trùng với mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(newPassword, user.MatKhau))
                return Json(new { success = false, message = "Mật khẩu mới phải khác mật khẩu hiện tại" });

            // Mã hóa và lưu mật khẩu mới
            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActivePromotions()
        {
            try
            {
                var now = DateTime.Now;
                var currentDayOfWeek = now.DayOfWeek;
                // Convert DayOfWeek to Vietnamese format: Monday=2, Sunday=CN
                string dayCode = currentDayOfWeek switch
                {
                    DayOfWeek.Monday => "2",
                    DayOfWeek.Tuesday => "3",
                    DayOfWeek.Wednesday => "4",
                    DayOfWeek.Thursday => "5",
                    DayOfWeek.Friday => "6",
                    DayOfWeek.Saturday => "7",
                    DayOfWeek.Sunday => "CN",
                    _ => "ALL"
                };

                var promotions = await _context.KhuyenMais
                    .Where(k => k.TrangThai == "DANG_HOAT_DONG" 
                        && k.NgayBatDau <= now 
                        && k.NgayKetThuc >= now
                        && (k.NgayApDung == null || k.NgayApDung == "ALL" || k.NgayApDung.Contains(dayCode)))
                    .OrderByDescending(k => k.UuTien)
                    .ThenBy(k => k.NgayKetThuc)
                    .Take(10)
                    .Select(k => new
                    {
                        k.IdKhuyenMai,
                        k.TenKhuyenMai,
                        k.MoTa,
                        k.LoaiKhuyenMai,
                        k.HinhThucGiam,
                        k.GiaTriGiam,
                        k.GiaTriGiamToiDa,
                        k.GiaTriDonHangToiThieu,
                        k.GiaTriGiamToiDaDonHang,
                        k.NgayBatDau,
                        k.NgayKetThuc,
                        k.NgayApDung
                    })
                    .ToListAsync();

                return Json(promotions);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }


    }
}












      