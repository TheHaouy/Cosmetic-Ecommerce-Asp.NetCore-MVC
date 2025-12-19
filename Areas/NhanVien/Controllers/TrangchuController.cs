using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using Final_VS1.Areas.NhanVien.Models;

namespace Final_VS1.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "Nhanvien,admin")]
    public class TrangchuController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public TrangchuController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                // Đơn hàng chờ xử lý
                DonHangChoXuLy = await _context.DonHangs
                    .CountAsync(d => d.TrangThai == "Chờ xác nhận"),

                // Đơn hàng đang giao
                DonHangDangGiao = await _context.DonHangs
                    .CountAsync(d => d.TrangThai == "Đang giao hàng"),

                // Đơn hàng hoàn thành hôm nay
                DonHangHoanThanh = await _context.DonHangs
                    .CountAsync(d => d.TrangThai == "Hoàn thành" && 
                                   d.NgayDat.HasValue && 
                                   d.NgayDat.Value.Date == DateTime.Today),

                // Sản phẩm sắp hết (< 10)
                SanPhamSapHet = await _context.BienTheSanPhams
                    .CountAsync(bt => bt.SoLuongTonKho < 10),

                // Đơn hàng mới nhất
                DonHangMoiNhat = await _context.DonHangs
                    .Include(d => d.IdTaiKhoanNavigation)
                    .Include(d => d.IdVcNavigation)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(10)
                    .ToListAsync(),

                // Sản phẩm sắp hết chi tiết
                SanPhamSapHetChiTiet = await _context.BienTheSanPhams
                    .Include(bt => bt.IdSanPhamNavigation)
                    .Where(bt => bt.SoLuongTonKho < 10)
                    .OrderBy(bt => bt.SoLuongTonKho)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
