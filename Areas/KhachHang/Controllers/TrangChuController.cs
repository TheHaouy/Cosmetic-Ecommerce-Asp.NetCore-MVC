using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using System.Threading.Tasks;
using System.Linq;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class TrangChuController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public TrangChuController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            // Lấy danh mục con (không lấy danh mục cha)
            var danhMucs = await _context.DanhMucs
                .Where(d => d.IdDanhMucCha != null) // Chỉ lấy danh mục con
                .OrderBy(d => d.ThuTuHienThi)
                .Take(6)
                .ToListAsync();

            // Tính số sản phẩm cho từng danh mục
            var danhMucVoiSoLuong = new System.Collections.Generic.List<dynamic>();
            foreach (var dm in danhMucs)
            {
                var soLuongSanPham = await _context.SanPhams
                    .Where(s => s.IdDanhMuc == dm.IdDanhMuc && s.TrangThai == true)
                    .CountAsync();

                danhMucVoiSoLuong.Add(new {
                    IdDanhMuc = dm.IdDanhMuc,
                    TenDanhMuc = dm.TenDanhMuc,
                    SoLuongSanPham = soLuongSanPham,
                    Slug = dm.DuongDanSeo ?? $"danh-muc-{dm.IdDanhMuc}" // Fallback nếu không có slug
                });
            }

            // Lấy sản phẩm bán chạy
            var topProductIds = await _context.ChiTietDonHangs
                .Join(_context.BienTheSanPhams, ctdh => ctdh.IdBienThe, bt => bt.IdBienThe, (ctdh, bt) => new { ctdh, bt })
                .Join(_context.SanPhams, x => x.bt.IdSanPham, sp => sp.IdSanPham, (x, sp) => new { x.ctdh, sp })
                .Where(x => x.sp.TrangThai == true)
                .GroupBy(x => x.sp.IdSanPham)
                .OrderByDescending(g => g.Sum(x => x.ctdh.SoLuong))
                .Select(g => g.Key)
                //.Take(4)
                .ToListAsync();

            if (topProductIds.Any())
            {
                var sanPhamBanChay = await _context.SanPhams
                    .Include(s => s.AnhSanPhams)
                    .Include(s => s.BienTheSanPhams)
                    .Where(s => topProductIds.Contains(s.IdSanPham))
                    .ToListAsync();

                var reviewStats = await _context.DanhGia
                    .Where(dg => dg.IdSanPham.HasValue && topProductIds.Contains(dg.IdSanPham.Value))
                    .GroupBy(dg => dg.IdSanPham.Value)
                    .Select(g => new {
                        IdSanPham = g.Key,
                        Count = g.Count(),
                        AverageRating = g.Average(x => (double?)x.SoSao) ?? 0
                    })
                    .ToListAsync();

                var sanPhamBanChayWithReview = sanPhamBanChay.Select(sp => new {
                    SanPham = sp,
                    ReviewCount = reviewStats.FirstOrDefault(r => r.IdSanPham == sp.IdSanPham)?.Count ?? 0,
                    AverageRating = reviewStats.FirstOrDefault(r => r.IdSanPham == sp.IdSanPham)?.AverageRating ?? 0
                }).ToList();

                ViewBag.SanPhamBanChay = sanPhamBanChayWithReview;
            }
            else
            {
                var sanPhamMoi = await _context.SanPhams
                    .Include(s => s.AnhSanPhams)
                    .Include(s => s.BienTheSanPhams)
                    .Where(s => s.TrangThai == true)
                    .OrderByDescending(s => s.NgayTao)
                    .Take(4)
                    .ToListAsync();

                var moiIds = sanPhamMoi.Select(sp => sp.IdSanPham).ToList();
                var reviewStats = await _context.DanhGia
                    .Where(dg => dg.IdSanPham.HasValue && moiIds.Contains(dg.IdSanPham.Value))
                    .GroupBy(dg => dg.IdSanPham.Value)
                    .Select(g => new {
                        IdSanPham = g.Key,
                        Count = g.Count(),
                        AverageRating = g.Average(x => (double?)x.SoSao) ?? 0
                    })
                    .ToListAsync();

                var sanPhamMoiWithReview = sanPhamMoi.Select(sp => new {
                    SanPham = sp,
                    ReviewCount = reviewStats.FirstOrDefault(r => r.IdSanPham == sp.IdSanPham)?.Count ?? 0,
                    AverageRating = reviewStats.FirstOrDefault(r => r.IdSanPham == sp.IdSanPham)?.AverageRating ?? 0
                }).ToList();

                ViewBag.SanPhamBanChay = sanPhamMoiWithReview;
            }

            ViewBag.DanhMucs = danhMucVoiSoLuong;

            return View();
        }

        [HttpPost]
        public IActionResult TimKiem(string searchQuery)
        {
            return RedirectToAction("Index", "SanPham", new { area = "KhachHang", search = searchQuery });
        }

        public IActionResult DanhMuc(string category)
        {
            return RedirectToAction("Category", "SanPham", new { area = "KhachHang", slug = category });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.DanhMucs
                    .Include(d => d.InverseIdDanhMucChaNavigation) // Load danh mục con
                    .Where(d => d.IdDanhMucCha == null) // Only parent categories
                    .OrderBy(d => d.ThuTuHienThi ?? 0)
                    .ThenBy(d => d.TenDanhMuc)
                    .ToListAsync();

                return PartialView("_CategoryDropdown", categories);
            }
            catch (Exception ex)
            {
                // Log error if needed
                return PartialView("_CategoryDropdown", new List<DanhMuc>());
            }
        }
    }
}
