using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "Nhanvien,admin")]
    public class SanphamController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public SanphamController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // GET: Danh sách sản phẩm (readonly)
        public async Task<IActionResult> Index()
        {
            var products = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.AnhSanPhams)
                .Include(s => s.BienTheSanPhams)
                .OrderByDescending(s => s.NgayTao)
                .ToListAsync();

            return View(products);
        }

        // GET: Chi tiết sản phẩm (readonly)
        public async Task<IActionResult> ChiTiet(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .Include(s => s.AnhSanPhams)
                .FirstOrDefaultAsync(s => s.IdSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }
    }
}
