using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class NguoidungController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public NguoidungController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // Trang danh sách khách hàng
        public async Task<IActionResult> Index()
        {
            var users = await _context.TaiKhoans
                .Where(t => t.VaiTro == "khach" || t.VaiTro == null)
                .Include(t => t.DonHangs)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();

            return View(users);
        }

        // Trang danh sách nhân viên
        public async Task<IActionResult> NhanVien()
        {
            var staffs = await _context.TaiKhoans
                .Where(t => t.VaiTro == "Nhanvien")
                .Include(t => t.DonHangs)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();

            return View(staffs);
        }

        // Trang chi tiết người dùng
        public async Task<IActionResult> Detail(int id)
        {
            var user = await _context.TaiKhoans
                .Include(t => t.DonHangs)
                    .ThenInclude(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation!)
                            .ThenInclude(bt => bt.IdSanPhamNavigation!)
                .Include(t => t.DonHangs)
                    .ThenInclude(d => d.IdDiaChiNavigation)
                .Include(t => t.DiaChis)
                .Include(t => t.DanhGia)
                    .ThenInclude(dg => dg.IdSanPhamNavigation)
                .FirstOrDefaultAsync(t => t.IdTaiKhoan == id);

            if (user == null)
            {
                return NotFound();
            }

            var logs = await _context.LogHoatDongs
                .Where(l => l.IdTaiKhoan == id)
                .OrderByDescending(l => l.ThoiGian)
                .Take(50)
                .ToListAsync();

            ViewBag.ActivityLogs = logs;
            return View(user);
        }

        // GET: Trang tạo nhân viên mới
        [HttpGet]
        public IActionResult ThemNhanVien()
        {
            return View();
        }

        // POST: Tạo nhân viên mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemNhanVien(string hoTen, string email, string matKhau, string? soDienThoai, string? gioiTinh, DateOnly? ngaySinh)
        {
            try
            {
                // Kiểm tra email đã tồn tại
                var existingUser = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email == email);
                if (existingUser != null)
                {
                    TempData["Error"] = "Email đã được sử dụng!";
                    return View();
                }

                // Mã hóa mật khẩu (sử dụng BCrypt hoặc phương thức mã hóa tương tự)
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(matKhau);

                var newStaff = new TaiKhoan
                {
                    HoTen = hoTen,
                    Email = email,
                    MatKhau = hashedPassword,
                    VaiTro = "Nhanvien", // Vai trò mặc định
                    SoDienThoai = soDienThoai,
                    GioiTinh = gioiTinh,
                    NgaySinh = ngaySinh,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                _context.TaiKhoans.Add(newStaff);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm nhân viên thành công!";
                return RedirectToAction("NhanVien");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // POST: Khóa/Mở khóa tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _context.TaiKhoans.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }

                // Không cho phép khóa tài khoản admin đang đăng nhập
                var currentUserEmail = User.Identity?.Name;
                if (user.Email == currentUserEmail)
                {
                    return Json(new { success = false, message = "Không thể khóa tài khoản đang đăng nhập!" });
                }

                // Toggle trạng thái
                user.TrangThai = !user.TrangThai;
                await _context.SaveChangesAsync();

                string action = user.TrangThai == true ? "mở khóa" : "khóa";
                return Json(new { success = true, message = $"Đã {action} tài khoản thành công!", status = user.TrangThai });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
