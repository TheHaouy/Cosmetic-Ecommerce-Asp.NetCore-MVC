using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Final_VS1.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "Nhanvien,admin")]
    public class KhachhangController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public KhachhangController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // GET: Danh sách khách hàng (readonly)
        public async Task<IActionResult> Index()
        {
            var customers = await _context.TaiKhoans
                .Where(t => t.VaiTro == "khach" || t.VaiTro == null)
                .Include(t => t.DonHangs)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();

            return View(customers);
        }

        // GET: Chi tiết khách hàng (readonly)
        public async Task<IActionResult> Detail(int id)
        {
            var customer = await _context.TaiKhoans
                .Include(t => t.DonHangs)
                    .ThenInclude(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                            .ThenInclude(bt => bt.IdSanPhamNavigation)
                .Include(t => t.DiaChis)
                .FirstOrDefaultAsync(t => t.IdTaiKhoan == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id, bool lockAccount, string? note)
        {
            var customer = await _context.TaiKhoans.FindAsync(id);
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng" });
            }

            if (lockAccount && string.IsNullOrWhiteSpace(note))
            {
                return Json(new { success = false, message = "Vui lòng nhập ghi chú khi khóa tài khoản" });
            }

            int? staffId = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var parsedId))
            {
                staffId = parsedId;
            }

            var targetStatus = lockAccount ? false : true;
            if (customer.TrangThai == targetStatus)
            {
                return Json(new { success = false, message = "Trạng thái tài khoản không thay đổi" });
            }

            customer.TrangThai = targetStatus;

            var log = new LogHoatDong
            {
                IdTaiKhoan = staffId,
                HanhDong = lockAccount ? "Khoa tai khoan khach" : "Mo khoa tai khoan khach",
                DoiTuong = "TaiKhoan",
                IdDoiTuong = id,
                ThoiGian = DateTime.Now,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            if (!string.IsNullOrWhiteSpace(note))
            {
                log.HanhDong += $": {note.Trim()}";
            }

            _context.LogHoatDongs.Add(log);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = lockAccount ? "Đã khóa tài khoản khách." : "Đã mở khóa tài khoản khách.",
                status = customer.TrangThai
            });
        }
    }
}
