using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using System.Threading.Tasks;
using System.Linq;
using Final_VS1.Helpers;
using Final_VS1.Areas.KhachHang.Models;
using Final_VS1.Models;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class SanPhamController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public SanPhamController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // Helper method để lấy ảnh sản phẩm theo thứ tự ưu tiên
        private List<string> GetProductImages(ICollection<AnhSanPham>? anhSanPhams)
        {
            var result = new List<string>();
            
            if (anhSanPhams != null && anhSanPhams.Any())
            {
                // Bước 1: Tìm ảnh chính (ưu tiên LinkCloudinary, sau đó DuongDan)
                var anhChinh = anhSanPhams.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.LoaiAnh) &&
                    (a.LoaiAnh.Trim().ToLower() == "chinh" || a.LoaiAnh.Trim().ToLower() == "chính"));
                    
                if (anhChinh != null)
                {
                    // Ưu tiên LinkCloudinary, nếu không có thì dùng DuongDan
                    if (!string.IsNullOrEmpty(anhChinh.LinkCloudinary))
                        result.Add(anhChinh.LinkCloudinary);
                    else if (!string.IsNullOrEmpty(anhChinh.DuongDan))
                        result.Add(anhChinh.DuongDan);
                }
                else
                {
                    // Bước 2: Nếu không có ảnh chính, tìm ảnh phụ
                    var anhPhu = anhSanPhams.FirstOrDefault(a => 
                        !string.IsNullOrEmpty(a.LoaiAnh) &&
                        (a.LoaiAnh.Trim().ToLower() == "phu" || a.LoaiAnh.Trim().ToLower() == "phụ"));
                        
                    if (anhPhu != null)
                    {
                        // Ưu tiên LinkCloudinary, nếu không có thì dùng DuongDan
                        if (!string.IsNullOrEmpty(anhPhu.LinkCloudinary))
                            result.Add(anhPhu.LinkCloudinary);
                        else if (!string.IsNullOrEmpty(anhPhu.DuongDan))
                            result.Add(anhPhu.DuongDan);
                    }
                }
                
                // Nếu vẫn chưa có ảnh, lấy ảnh đầu tiên
                if (result.Count == 0)
                {
                    var anyImage = anhSanPhams.FirstOrDefault(a => 
                        !string.IsNullOrEmpty(a.LinkCloudinary) || !string.IsNullOrEmpty(a.DuongDan));
                    
                    if (anyImage != null)
                    {
                        if (!string.IsNullOrEmpty(anyImage.LinkCloudinary))
                            result.Add(anyImage.LinkCloudinary);
                        else if (!string.IsNullOrEmpty(anyImage.DuongDan))
                            result.Add(anyImage.DuongDan);
                    }
                }
            }
            
            // Nếu không có ảnh nào, trả về list rỗng (sẽ dùng default trong View)
            return result;
        }

        public async Task<IActionResult> Index(string search, string category, decimal? minPrice, decimal? maxPrice, string sortBy, int page = 1)
        {
            const int pageSize = 9;

            var query = _context.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                .Where(s => s.TrangThai == true);

            // Tìm kiếm theo từ khóa - improved search
            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLower();
                query = query.Where(s =>
                    (s.TenSanPham ?? "").ToLower().Contains(searchLower) ||
                    (s.MoTa ?? "").ToLower().Contains(searchLower) ||
                    (s.IdDanhMucNavigation.TenDanhMuc ?? "").ToLower().Contains(searchLower));
                ViewBag.SearchQuery = search;
            }

            // Lọc theo danh mục
            DanhMuc? selectedCategory = null;
if (!string.IsNullOrEmpty(category))
{
    // Thử tìm theo slug trước
    selectedCategory = await _context.DanhMucs
        .Include(d => d.InverseIdDanhMucChaNavigation) // Load danh mục con
        .FirstOrDefaultAsync(d => d.DuongDanSeo == category);
    
    // Nếu không tìm thấy theo slug, thử parse làm ID (legacy support)
    if (selectedCategory == null && int.TryParse(category, out int categoryId))
    {
        selectedCategory = await _context.DanhMucs
            .Include(d => d.InverseIdDanhMucChaNavigation)
            .FirstOrDefaultAsync(d => d.IdDanhMuc == categoryId);
        
        // Nếu tìm thấy và có slug, redirect về URL đẹp
        if (selectedCategory != null && !string.IsNullOrEmpty(selectedCategory.DuongDanSeo))
        {
            return RedirectPermanent($"/danh-muc/{selectedCategory.DuongDanSeo}?page={page}" +
                (!string.IsNullOrEmpty(search) ? $"&search={search}" : "") +
                (minPrice.HasValue ? $"&minPrice={minPrice}" : "") +
                (maxPrice.HasValue ? $"&maxPrice={maxPrice}" : "") +
                (!string.IsNullOrEmpty(sortBy) ? $"&sortBy={sortBy}" : ""));
        }
    }
    
    if (selectedCategory != null)
    {
        // Nếu là danh mục cha (có danh mục con), lấy tất cả sản phẩm trong các danh mục con
        if (selectedCategory.InverseIdDanhMucChaNavigation.Any())
        {
            var childCategoryIds = selectedCategory.InverseIdDanhMucChaNavigation
                .Select(c => c.IdDanhMuc)
                .ToList();
            childCategoryIds.Add(selectedCategory.IdDanhMuc); // Thêm cả danh mục cha
            
            query = query.Where(s => childCategoryIds.Contains(s.IdDanhMuc ?? 0));
        }
        else
        {
            // Nếu là danh mục con, chỉ lấy sản phẩm trong danh mục đó
            query = query.Where(s => s.IdDanhMuc == selectedCategory.IdDanhMuc);
        }
        ViewBag.CurrentCategory = selectedCategory;
    }
}
            // Lọc theo giá - now using variant prices
            if (minPrice.HasValue)
            {
                query = query.Where(s => s.BienTheSanPhams.Any(bt => bt.GiaBan >= minPrice.Value));
                ViewBag.MinPrice = minPrice;
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(s => s.BienTheSanPhams.Any(bt => bt.GiaBan <= maxPrice.Value));
                ViewBag.MaxPrice = maxPrice;
            }

            // Sắp xếp
            List<Areas.KhachHang.Models.SanPhamViewModel> sanPhamViewModels = new List<Areas.KhachHang.Models.SanPhamViewModel>();
            int totalProducts = 0;
            int totalPages = 0;
            int skip = 0;

            switch (sortBy?.ToLower())
            {
                case "newest":
                    query = query.OrderByDescending(s => s.NgayTao);
                    break;
                case "price-asc":
                    // Sắp xếp theo giá thấp đến cao - dùng giá MIN (giá hiển thị trên card)
                    query = query.Where(s => s.BienTheSanPhams.Any())
                                 .OrderBy(s => s.BienTheSanPhams.Min(bt => bt.GiaBan));
                    break;
                case "price-desc":
                    // Sắp xếp theo giá cao đến thấp - CŨNG dùng giá MIN (giá hiển thị trên card)
                    // Để người dùng thấy logic: sản phẩm hiển thị 400k sẽ ở trước sản phẩm 140k
                    query = query.Where(s => s.BienTheSanPhams.Any())
                                 .OrderByDescending(s => s.BienTheSanPhams.Min(bt => bt.GiaBan));
                    break;
                case "bestseller":
                    var bestSellerIds = _context.ChiTietDonHangs
                        .Join(_context.BienTheSanPhams, ctdh => ctdh.IdBienThe, bt => bt.IdBienThe, (ctdh, bt) => new { ctdh, bt })
                        .Join(_context.SanPhams, x => x.bt.IdSanPham, sp => sp.IdSanPham, (x, sp) => new { x.ctdh, sp })
                        .Where(x => x.sp.TrangThai == true)
                        .GroupBy(x => x.sp.IdSanPham)
                        .OrderByDescending(g => g.Sum(x => x.ctdh.SoLuong))
                        .Select(g => g.Key)
                        .ToList();

                    var allProducts = await query.Where(s => bestSellerIds.Contains(s.IdSanPham)).ToListAsync();
                    var sortedProducts = allProducts
                        .OrderBy(s => bestSellerIds.IndexOf(s.IdSanPham))
                        .ToList();

                    totalProducts = sortedProducts.Count;
                    totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                    skip = (page - 1) * pageSize;
                    var pagedProducts = sortedProducts.Skip(skip).Take(pageSize).ToList();

                    sanPhamViewModels = pagedProducts.Select(sp => new Areas.KhachHang.Models.SanPhamViewModel
                    {
                        IdSanPham = sp.IdSanPham,
                        TenSanPham = sp.TenSanPham,
                        Slug = sp.Slug,
                        MoTa = sp.MoTa,
                        GiaBan = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Min(bt => bt.GiaBan) : null,
                        MaxPrice = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Max(bt => bt.GiaBan) : null,
                        TrangThai = sp.TrangThai,
                        IdDanhMuc = sp.IdDanhMuc,
                        CachSuDung = sp.CachSuDung,
                        NgayTao = sp.NgayTao,
                        TenDanhMuc = sp.IdDanhMucNavigation?.TenDanhMuc,
                        AnhChinhs = GetProductImages(sp.AnhSanPhams),
                        DiemDanhGia = sp.DanhGia != null && sp.DanhGia.Count > 0 ? sp.DanhGia.Average(dg => dg.SoSao ?? 0) : 0,
                        SoLuongDanhGia = sp.DanhGia?.Count ?? 0,
                        IdBienTheGiaThapNhat = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.OrderBy(bt => bt.GiaBan).First().IdBienThe : (int?)null,
                        HasVariants = sp.BienTheSanPhams.Any()
                    }).ToList();
                    break;
                default:
                    // Sắp xếp mặc định: mới nhất
                    query = query.OrderByDescending(s => s.NgayTao);
                    break;
            }

            if (sortBy?.ToLower() != "bestseller")
            {
                totalProducts = await query.CountAsync();
                totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                skip = (page - 1) * pageSize;
                var sanPhams = await query.Skip(skip).Take(pageSize).ToListAsync();
                sanPhamViewModels = sanPhams.Select(sp => new Areas.KhachHang.Models.SanPhamViewModel
                {
                    IdSanPham = sp.IdSanPham,
                    TenSanPham = sp.TenSanPham,
                    Slug = sp.Slug,
                    MoTa = sp.MoTa,
                    GiaBan = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Min(bt => bt.GiaBan) : null,
                    MaxPrice = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Max(bt => bt.GiaBan) : null,
                    TrangThai = sp.TrangThai,
                    IdDanhMuc = sp.IdDanhMuc,
                    CachSuDung = sp.CachSuDung,
                    NgayTao = sp.NgayTao,
                    TenDanhMuc = sp.IdDanhMucNavigation?.TenDanhMuc,
                    AnhChinhs = GetProductImages(sp.AnhSanPhams),
                    DiemDanhGia = sp.DanhGia != null && sp.DanhGia.Count > 0 ? sp.DanhGia.Average(dg => dg.SoSao ?? 0) : 0,
                    SoLuongDanhGia = sp.DanhGia?.Count ?? 0,
                    IdBienTheGiaThapNhat = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.OrderBy(bt => bt.GiaBan).First().IdBienThe : (int?)null,
                    HasVariants = sp.BienTheSanPhams.Any()
                }).ToList();
            }

            // Thêm thông tin khuyến mãi cho từng sản phẩm
            foreach (var sp in sanPhamViewModels)
            {
                var khuyenMai = await PromotionHelper.GetBestPromotionForProduct(_context, sp.IdSanPham, sp.IdDanhMuc);
                if (khuyenMai != null && sp.GiaBan.HasValue)
                {
                    sp.GiaKhuyenMai = PromotionHelper.CalculatePromotionPrice(sp.GiaBan.Value, khuyenMai);
                    sp.PhanTramGiam = PromotionHelper.CalculateDiscountPercentage(sp.GiaBan.Value, sp.GiaKhuyenMai.Value);
                    sp.TenKhuyenMai = khuyenMai.TenKhuyenMai;
                    sp.IsFlashSale = PromotionHelper.IsFlashSale(khuyenMai);
                }
            }

            // Lấy danh sách danh mục cho sidebar (bao gồm danh mục con)
            var danhMucs = await _context.DanhMucs
                .Include(d => d.InverseIdDanhMucChaNavigation) // Load danh mục con
                .Where(d => d.IdDanhMucCha == null)
                .OrderBy(d => d.ThuTuHienThi)
                .ToListAsync();

            ViewBag.DanhMucs = danhMucs;
            ViewBag.SortBy = sortBy;

            // Thông tin phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.PageSize = pageSize;

            // Giữ lại các tham số filter cho phân trang
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategoryString = category;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentSortBy = sortBy;

            return View(sanPhamViewModels);
        }

        public IActionResult TimKiem(string searchTerm)
        {
            return RedirectToAction("Index", new { search = searchTerm });
        }

        public IActionResult DanhMuc(string category)
        {
            return RedirectToAction("Index", new { category = category });
        }

        public IActionResult LuocGia(decimal? minPrice, decimal? maxPrice)
        {
            return RedirectToAction("Index", new { minPrice = minPrice, maxPrice = maxPrice });
        }

        public IActionResult SapXep(string sortBy)
        {
            return RedirectToAction("Index", new { sortBy = sortBy });
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                {
                    return Json(new List<object>());
                }

                // Normalize search term
                var normalizedTerm = VietnameseTextHelper.RemoveDiacritics(term.Trim().ToLower());

                var queryResults = await _context.SanPhams
                    .Include(s => s.IdDanhMucNavigation)
                    .Include(s => s.AnhSanPhams)
                    .Where(s => s.TrangThai == true) // Only active products
                    .Select(s => new
                    {
                        s.IdSanPham,
                        s.TenSanPham,
                        GiaBan = s.BienTheSanPhams.Any() ? s.BienTheSanPhams.Min(bt => bt.GiaBan) : (decimal?)null,
                        DanhMuc = s.IdDanhMucNavigation != null ? s.IdDanhMucNavigation.TenDanhMuc : "",
                        AnhChinh = s.AnhSanPhams
                            .Where(a => a.LoaiAnh == "Chinh" || a.LoaiAnh == "Primary" || a.LoaiAnh == "Main" || a.LoaiAnh == "Chính")
                            .Select(a => a.DuongDan)
                            .FirstOrDefault() ??
                            s.AnhSanPhams.Select(a => a.DuongDan).FirstOrDefault(),
                        // Create normalized search field
                        NormalizedName = s.TenSanPham != null ? s.TenSanPham.ToLower() : ""
                    })
                    .ToListAsync();

                var suggestions = queryResults
                    .Where(s => VietnameseTextHelper.RemoveDiacritics(s.NormalizedName).Contains(normalizedTerm))
                    .Take(8) // Limit to 8 suggestions
                    .Select(s => new
                    {
                        name = s.TenSanPham,
                        price = s.GiaBan,
                        category = s.DanhMuc,
                        image = s.AnhChinh,
                        url = Url.Action("Index", "Chitiet", new { area = "KhachHang", id = s.IdSanPham })
                    })
                    .ToList();

                return Json(suggestions);
            }
            catch (Exception ex)
            {
                // Log error if needed
                return Json(new List<object>());
            }
        }

        // Action để lấy top 6 danh mục bán chạy nhất cho footer
        [HttpGet]
        public async Task<IActionResult> GetTopCategories()
        {
            try
            {
                // Lấy top 6 danh mục có nhiều sản phẩm bán nhất
                var topCategories = await _context.ChiTietDonHangs
                    .Join(_context.BienTheSanPhams, ctdh => ctdh.IdBienThe, bt => bt.IdBienThe, (ctdh, bt) => new { ctdh, bt })
                    .Join(_context.SanPhams, x => x.bt.IdSanPham, sp => sp.IdSanPham, (x, sp) => new { x.ctdh, sp })
                    .Join(_context.DanhMucs, x => x.sp.IdDanhMuc, dm => dm.IdDanhMuc, (x, dm) => new { x.ctdh, x.sp, dm })
                    .Where(x => x.sp.TrangThai == true && x.dm.IdDanhMucCha == null) // Chỉ lấy danh mục cha
                    .GroupBy(x => new { x.dm.IdDanhMuc, x.dm.TenDanhMuc })
                    .Select(g => new
                    {
                        IdDanhMuc = g.Key.IdDanhMuc,
                        TenDanhMuc = g.Key.TenDanhMuc,
                        SoLuongBan = g.Sum(x => x.ctdh.SoLuong)
                    })
                    .OrderByDescending(x => x.SoLuongBan)
                    .Take(6)
                    .ToListAsync();

                return Json(topCategories);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        // ===== SEO-FRIENDLY SLUG-BASED ACTIONS =====

        /// <summary>
        /// Display product details by slug (SEO-friendly URL)
        /// Route: /san-pham/{slug}
        /// Example: /san-pham/kem-duong-da-mat
        /// KHÔNG REDIRECT - Trả về View trực tiếp để giữ URL đẹp
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            // Tìm sản phẩm theo slug với đầy đủ thông tin
            var sanPham = await _context.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                    .ThenInclude(d => d.IdTaiKhoanNavigation)
                .Include(s => s.IdDanhMucNavigation)
                    .ThenInclude(dm => dm!.IdDanhMucChaNavigation)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .FirstOrDefaultAsync(s => s.Slug == slug && s.TrangThai == true);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Lưu danh mục cha vào ViewBag
            if (sanPham.IdDanhMucNavigation?.IdDanhMucChaNavigation != null)
            {
                ViewBag.DanhMucCha = sanPham.IdDanhMucNavigation.IdDanhMucChaNavigation;
            }

            var tongSoLuongTonKho = sanPham.BienTheSanPhams?.Sum(bt => bt.SoLuongTonKho) ?? 0;
            ViewBag.TongSoLuongTonKho = tongSoLuongTonKho;

            // Lấy khuyến mãi cho sản phẩm (chỉ truyền thông tin khuyến mãi, không tính giá cụ thể)
            var khuyenMai = await Helpers.PromotionHelper.GetBestPromotionForProduct(_context, sanPham.IdSanPham, sanPham.IdDanhMuc);
            ViewBag.KhuyenMai = khuyenMai;
            
            Console.WriteLine($"[SanPham.Details] Product: {sanPham.TenSanPham}, Promotion: {khuyenMai?.TenKhuyenMai ?? "None"}");
            Console.WriteLine($"[SanPham.Details] BienTheSanPhams Count: {sanPham.BienTheSanPhams?.Count ?? 0}");
            if (sanPham.BienTheSanPhams != null)
            {
                foreach (var bt in sanPham.BienTheSanPhams)
                {
                    Console.WriteLine($"[SanPham.Details] Variant {bt.IdBienThe}: Price={bt.GiaBan}, Stock={bt.SoLuongTonKho}, Attributes={bt.IdGiaTris?.Count ?? 0}");
                }
            }
            
            // KHÔNG tính giá chung nữa - JavaScript sẽ tính giá riêng cho từng biến thể
            // Điều này cho phép mỗi biến thể có giá khuyến mãi khác nhau

            var danhGias = sanPham.DanhGia?
                .Where(d => (d.SoSao ?? 0) > 0)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToList() ?? new List<DanhGium>();
            ViewBag.TongSoDanhGia = danhGias.Count;
            ViewBag.DiemTrungBinh = danhGias.Any() ? Math.Round(danhGias.Average(d => (d.SoSao ?? 0)), 1) : 0;

            ViewBag.ThongKeSao = new Dictionary<int, int>
            {
                { 5, danhGias.Count(d => d.SoSao == 5) },
                { 4, danhGias.Count(d => d.SoSao == 4) },
                { 3, danhGias.Count(d => d.SoSao == 3) },
                { 2, danhGias.Count(d => d.SoSao == 2) },
                { 1, danhGias.Count(d => d.SoSao == 1) }
            };

            // Lấy sản phẩm gợi ý
            var sanPhamGoiY = await _context.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                .Include(s => s.BienTheSanPhams)
                .Where(s => s.IdDanhMuc == sanPham.IdDanhMuc &&
                           s.IdSanPham != sanPham.IdSanPham &&
                           s.TrangThai == true)
                .Take(12)
                .ToListAsync();

            var sanPhamGoiYViewModel = sanPhamGoiY.Select(sp => new ChiTietSanPhamViewModel
            {
                Id = sp.IdSanPham,
                TenSanPham = sp.TenSanPham ?? "",
                Slug = sp.Slug,
                GiaBan = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Min(bt => bt.GiaBan) : null,
                MaxPrice = sp.BienTheSanPhams.Any() ? sp.BienTheSanPhams.Max(bt => bt.GiaBan) : null,
                AnhSanPhams = sp.AnhSanPhams?.Select(a => new AnhSanPhamViewModel
                {
                    IdAnh = a.IdAnh,
                    DuongDan = a.DuongDan ?? "",
                    LoaiAnh = a.LoaiAnh ?? ""
                }).ToList() ?? new List<AnhSanPhamViewModel>(),
                DanhGias = sp.DanhGia?.Where(d => (d.SoSao ?? 0) > 0).Select(d => new DanhGiaViewModel
                {
                    IdDanhGia = d.IdDanhGia,
                    IdTaiKhoan = d.IdTaiKhoan ?? 0,
                    SoSao = d.SoSao,
                    BinhLuan = d.BinhLuan ?? "",
                    AnhDanhGia = d.AnhDanhGia ?? "",
                    NgayDanhGia = d.NgayDanhGia,
                    HoTen = d.IdTaiKhoanNavigation?.HoTen ?? "Người dùng ẩn danh",
                    TraLoiCuaShop = d.TraLoiCuaShop,
                    NgayTraLoi = d.NgayTraLoi
                }).ToList() ?? new List<DanhGiaViewModel>()
            }).ToList();

            ViewBag.SanPhamGoiY = sanPhamGoiYViewModel;
            ViewBag.ProductId = sanPham.IdSanPham;

            // Lấy userId từ đăng nhập
            int? userId = null;
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                if (claim != null && int.TryParse(claim.Value, out int uid))
                {
                    userId = uid;
                }
            }

            // Sắp xếp reviews
            if (userId.HasValue)
            {
                danhGias = danhGias
                    .OrderByDescending(d => d.IdTaiKhoan == userId.Value)
                    .ThenByDescending(d => d.NgayDanhGia)
                    .ToList();
            }

            bool daMuaSanPham = false;
            bool daDanhGiaLanMuaCuoi = false;

            if (userId.HasValue)
            {
                var donHangIds = _context.DonHangs
                    .Where(dh => dh.IdTaiKhoan == userId.Value && dh.TrangThai == "hoàn thành")
                    .Select(dh => dh.IdDonHang)
                    .ToList();

                var chiTietDonHangs = _context.ChiTietDonHangs
                    .Where(ct => ct.IdDonHang.HasValue && donHangIds.Contains(ct.IdDonHang.Value))
                    .ToList();

                var bienTheIds = _context.BienTheSanPhams
                    .Where(bt => bt.IdSanPham == sanPham.IdSanPham)
                    .Select(bt => bt.IdBienThe)
                    .ToList();

                var lanMuaCuoi = chiTietDonHangs
                    .Where(ct => ct.IdBienThe.HasValue && bienTheIds.Contains(ct.IdBienThe.Value))
                    .OrderByDescending(ct => ct.IdDonHang)
                    .FirstOrDefault();

                daMuaSanPham = lanMuaCuoi != null;

                if (daMuaSanPham && lanMuaCuoi != null && lanMuaCuoi.IdDonHang.HasValue)
                {
                    var ngayMuaCuoi = _context.DonHangs
                        .Where(dh => dh.IdDonHang == lanMuaCuoi.IdDonHang.Value)
                        .Select(dh => dh.NgayDat)
                        .FirstOrDefault();

                    if (ngayMuaCuoi != null)
                    {
                        daDanhGiaLanMuaCuoi = _context.DanhGia
                            .Any(dg => dg.IdTaiKhoan == userId.Value &&
                                      dg.IdSanPham == sanPham.IdSanPham &&
                                      dg.NgayDanhGia >= ngayMuaCuoi);
                    }
                }
            }

            ViewBag.DaMuaSanPham = daMuaSanPham;
            ViewBag.DaDanhGiaLanMuaCuoi = daDanhGiaLanMuaCuoi;

            // === SEO META TAGS & OPEN GRAPH ===
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var productUrl = SeoHelper.GetCanonicalUrl(baseUrl, slug);
            
            // Lấy ảnh chính cho SEO
            var mainImage = GetProductImages(sanPham.AnhSanPhams).FirstOrDefault();
            
            // Nếu không có ảnh, dùng logo default
            if (string.IsNullOrEmpty(mainImage))
            {
                mainImage = "/Images/logomini.png"; // Hoặc logo.png nếu có
            }
            
            // Convert sang absolute URL (chỉ khi chưa phải absolute URL)
            var absoluteImageUrl = mainImage.StartsWith("http://") || mainImage.StartsWith("https://")
                ? mainImage
                : SeoHelper.GetAbsoluteUrl(baseUrl, mainImage);
            
            // Debug log
            Console.WriteLine($"[SEO DEBUG] Main Image: {mainImage}");
            Console.WriteLine($"[SEO DEBUG] Absolute URL: {absoluteImageUrl}");
            
            // Tính giá và rating
            var minPrice = sanPham.BienTheSanPhams.Any() ? sanPham.BienTheSanPhams.Min(bt => bt.GiaBan) ?? 0 : 0m;
            var avgRating = danhGias.Any() ? Math.Round(danhGias.Average(d => (d.SoSao ?? 0)), 1) : 0;
            var reviewCount = danhGias.Count;
            
            // Tạo SEO ViewModel
            var seoData = new SeoViewModel
            {
                Title = $"{sanPham.TenSanPham} - LittleFish Beauty",
                Description = SeoHelper.CreateMetaDescription(sanPham.MoTa, 200),
                Keywords = SeoHelper.GenerateKeywords(sanPham, sanPham.IdDanhMucNavigation),
                CanonicalUrl = productUrl,
                
                // Open Graph
                OgTitle = sanPham.TenSanPham,
                OgDescription = SeoHelper.CreateMetaDescription(sanPham.MoTa, 200),
                OgImage = absoluteImageUrl,
                OgUrl = productUrl,
                OgType = "product",
                
                // Product specific
                Price = minPrice,
                Currency = "VND",
                Availability = tongSoLuongTonKho > 0 ? "in stock" : "out of stock",
                Brand = "LittleFish Beauty",
                Category = sanPham.IdDanhMucNavigation?.TenDanhMuc,
                ImageAlt = sanPham.TenSanPham
            };
            
            ViewBag.SeoData = seoData;
            
            // === STRUCTURED DATA (JSON-LD) ===
            var structuredData = SeoHelper.GenerateProductStructuredData(
                sanPham,
                absoluteImageUrl,
                minPrice,
                tongSoLuongTonKho,
                reviewCount > 0 ? avgRating : null,
                reviewCount > 0 ? reviewCount : null,
                productUrl
            );
            
            ViewBag.StructuredData = structuredData;
            
            // Breadcrumb structured data
            var breadcrumbs = new List<(string Name, string Url)>
            {
                ("Trang chủ", "/"),
                ("Sản phẩm", "/KhachHang/SanPham")
            };
            
            if (sanPham.IdDanhMucNavigation != null)
            {
                breadcrumbs.Add((sanPham.IdDanhMucNavigation.TenDanhMuc ?? "Danh mục", 
                    $"/danh-muc/{sanPham.IdDanhMucNavigation.DuongDanSeo}"));
            }
            
            breadcrumbs.Add((sanPham.TenSanPham ?? "Sản phẩm", $"/san-pham/{slug}"));
            
            ViewBag.BreadcrumbStructuredData = SeoHelper.GenerateBreadcrumbStructuredData(breadcrumbs, baseUrl);

            // Tạo ViewModel
            var model = new ChiTietSanPhamViewModel
            {
                Id = sanPham.IdSanPham,
                IdDanhMuc = sanPham.IdDanhMuc ?? 0,
                TenSanPham = sanPham.TenSanPham ?? "",
                Slug = sanPham.Slug,
                MoTa = sanPham.MoTa ?? "",
                GiaBan = sanPham.BienTheSanPhams.Any() ? sanPham.BienTheSanPhams.Min(bt => bt.GiaBan) : null,
                MaxPrice = sanPham.BienTheSanPhams.Any() ? sanPham.BienTheSanPhams.Max(bt => bt.GiaBan) : null,
                TrangThai = sanPham.TrangThai,
                CachSuDung = sanPham.CachSuDung ?? "",
                NgayTao = sanPham.NgayTao,
                TenDanhMuc = sanPham.IdDanhMucNavigation?.TenDanhMuc ?? "",
                XuatXu = "Việt Nam",
                ThanhPhanChinh = sanPham.IdThanhPhans != null ? string.Join(", ", sanPham.IdThanhPhans.Select(tp => tp.TenThanhPhan)) : "",
                AnhSanPhams = sanPham.AnhSanPhams?.Select(a => new AnhSanPhamViewModel
                {
                    IdAnh = a.IdAnh,
                    DuongDan = a.DuongDan ?? "",
                    LoaiAnh = a.LoaiAnh ?? ""
                }).ToList() ?? new List<AnhSanPhamViewModel>(),
                BienTheSanPhams = sanPham.BienTheSanPhams?.Select(bt => new BienTheSanPhamViewModel
                {
                    IdBienThe = bt.IdBienThe,
                    Sku = bt.Sku ?? "",
                    SoLuongTonKho = bt.SoLuongTonKho,
                    GiaBan = bt.GiaBan,
                    ThuocTinhGiaTris = bt.IdGiaTris?.Select(gt => new ThuocTinhGiaTriViewModel
                    {
                        IdThuocTinh = gt.IdThuocTinhNavigation?.IdThuocTinh ?? 0,
                        TenThuocTinh = gt.IdThuocTinhNavigation?.TenThuocTinh ?? "",
                        IdGiaTri = gt.IdGiaTri,
                        GiaTri = gt.GiaTri ?? ""
                    }).ToList() ?? new List<ThuocTinhGiaTriViewModel>()
                }).ToList() ?? new List<BienTheSanPhamViewModel>(),
                DanhGias = danhGias.Select(d => new DanhGiaViewModel
                {
                    IdDanhGia = d.IdDanhGia,
                    IdTaiKhoan = d.IdTaiKhoan ?? 0,
                    SoSao = d.SoSao,
                    BinhLuan = d.BinhLuan ?? "",
                    AnhDanhGia = d.AnhDanhGia ?? "",
                    NgayDanhGia = d.NgayDanhGia,
                    HoTen = d.IdTaiKhoanNavigation?.HoTen ?? "Người dùng ẩn danh",
                    TraLoiCuaShop = d.TraLoiCuaShop,
                    NgayTraLoi = d.NgayTraLoi
                }).ToList()
            };

            Console.WriteLine($"[SanPham.Details] Model.BienTheSanPhams Count: {model.BienTheSanPhams?.Count ?? 0}");
            if (model.BienTheSanPhams != null)
            {
                foreach (var bt in model.BienTheSanPhams)
                {
                    Console.WriteLine($"[SanPham.Details] Model Variant {bt.IdBienThe}: Attributes={bt.ThuocTinhGiaTris?.Count ?? 0}");
                }
            }

            // TRẢ VỀ VIEW TRỰC TIẾP - KHÔNG REDIRECT
            return View("~/Areas/KhachHang/Views/ChiTiet/Index.cshtml", model);
        }

        /// <summary>
        /// Display products by category slug (SEO-friendly URL)
        /// Route: /danh-muc/{slug}
        /// Example: /danh-muc/cham-soc-da
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Category(string slug, string search, decimal? minPrice, decimal? maxPrice, string sortBy, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            // Verify category exists
            var categoryExists = await _context.DanhMucs
                .AnyAsync(d => d.DuongDanSeo == slug);

            if (!categoryExists)
            {
                return NotFound();
            }

            // Redirect to Index with category slug (not ID)
            return RedirectToAction("Index", new 
            { 
                category = slug,  // Pass slug instead of ID
                search, 
                minPrice, 
                maxPrice, 
                sortBy, 
                page 
            });
        }

        /// <summary>
        /// 301 Permanent Redirect from old ID-based URLs to new slug-based URLs
        /// Route: /KhachHang/ChiTiet/Index/{id}
        /// Redirects to: /san-pham/{slug}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RedirectToSlug(int id)
        {
            var sanPham = await _context.SanPhams
                .FirstOrDefaultAsync(s => s.IdSanPham == id);

            if (sanPham == null || string.IsNullOrWhiteSpace(sanPham.Slug))
            {
                // Fallback to old URL if slug not found
                return RedirectToAction("Index", "ChiTiet", new { area = "KhachHang", id });
            }

            // 301 Permanent Redirect to SEO-friendly URL
            return RedirectPermanent($"/san-pham/{sanPham.Slug}");
        }

        /// <summary>
        /// 301 Permanent Redirect from old category ID URLs to new slug-based URLs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RedirectCategoryToSlug(int id)
        {
            var category = await _context.DanhMucs
                .FirstOrDefaultAsync(d => d.IdDanhMuc == id);

            if (category == null || string.IsNullOrWhiteSpace(category.DuongDanSeo))
            {
                // Fallback to old URL if slug not found
                return RedirectToAction("Index", new { category = id.ToString() });
            }

            // 301 Permanent Redirect to SEO-friendly URL
            return RedirectPermanent($"/danh-muc/{category.DuongDanSeo}");
        }
    }
}
