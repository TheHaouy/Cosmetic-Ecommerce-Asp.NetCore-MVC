using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Final_VS1.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "Nhanvien,admin")]
    public class DonhangController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DonhangController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // GET: Danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            var orders = await _context.DonHangs
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdVcNavigation)
                .Include(d => d.ChiTietDonHangs)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(orders);
        }

        // GET: Chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.DonHangs
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdVcNavigation)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation)
                        .ThenInclude(bt => bt.IdSanPhamNavigation)
                            .ThenInclude(sp => sp.AnhSanPhams)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation)
                        .ThenInclude(bt => bt.IdGiaTris)
                            .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .Include(d => d.TimelineDonHangs)
                .Include(d => d.PhanHoiDonHangs)
                .FirstOrDefaultAsync(d => d.IdDonHang == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? note)
        {
            try
            {
                int? staffId = null;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out var parsedId))
                {
                    staffId = parsedId;
                }

                var order = await _context.DonHangs.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                var oldStatus = order.TrangThai;

                // Cập nhật trạng thái
                order.TrangThai = status;

                // Thêm vào timeline
                var timeline = new TimelineDonHang
                {
                    IdDonHang = id,
                    TrangThaiMoi = status,
                    GhiChu = !string.IsNullOrWhiteSpace(note)
                        ? note
                        : $"Đổi trạng thái từ '{oldStatus ?? "N/A"}' sang '{status}'",
                    NgayCapNhat = DateTime.Now
                };
                _context.TimelineDonHangs.Add(timeline);

                // Ghi log hoạt động của nhân viên
                var log = new LogHoatDong
                {
                    IdTaiKhoan = staffId,
                    HanhDong = "Cap nhat trang thai don hang",
                    DoiTuong = "DonHang",
                    IdDoiTuong = id,
                    ThoiGian = DateTime.Now,
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.LogHoatDongs.Add(log);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
