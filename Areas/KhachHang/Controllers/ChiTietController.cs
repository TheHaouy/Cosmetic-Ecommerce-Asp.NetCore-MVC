using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Final_VS1.Areas.KhachHang.Models;
using Final_VS1.Helpers;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class ChiTietController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public ChiTietController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // Helper method để lấy ảnh chính của sản phẩm theo thứ tự ưu tiên
        private string GetProductMainImage(ICollection<AnhSanPham>? anhSanPhams)
        {
            if (anhSanPhams != null && anhSanPhams.Any())
            {
                // Bước 1: Tìm ảnh chính
                var anhChinh = anhSanPhams.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.LoaiAnh) && !string.IsNullOrEmpty(a.DuongDan) &&
                    (a.LoaiAnh.Trim().ToLower() == "chinh" || a.LoaiAnh.Trim().ToLower() == "chính"));
                    
                if (anhChinh != null)
                {
                    return anhChinh.DuongDan!;
                }
                else
                {
                    // Bước 2: Nếu không có ảnh chính, tìm ảnh phụ
                    var anhPhu = anhSanPhams.FirstOrDefault(a => 
                        !string.IsNullOrEmpty(a.LoaiAnh) && !string.IsNullOrEmpty(a.DuongDan) &&
                        (a.LoaiAnh.Trim().ToLower() == "phu" || a.LoaiAnh.Trim().ToLower() == "phụ"));
                        
                    if (anhPhu != null)
                    {
                        return anhPhu.DuongDan!;
                    }
                }
            }
            
            // Nếu không có ảnh nào, trả về ảnh mặc định
            return "/images/noimage.jpg";
        }

        public async Task<IActionResult> Index(int id)
        {
            // Kiểm tra sản phẩm có slug không, nếu có thì 301 redirect về URL slug
            var sanPhamCheck = await _context.SanPhams
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSanPham == id);
            
            if (sanPhamCheck != null && !string.IsNullOrEmpty(sanPhamCheck.Slug))
            {
                // 301 Permanent Redirect về URL đẹp
                return RedirectPermanent($"/san-pham/{sanPhamCheck.Slug}");
            }

            // Nếu không có slug, tiếp tục xử lý bình thường (legacy support)
            var sanPham = await _context.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                    .ThenInclude(d => d.IdTaiKhoanNavigation)
                .Include(s => s.IdDanhMucNavigation)
                    .ThenInclude(dm => dm!.IdDanhMucChaNavigation) // Load danh mục cha
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .FirstOrDefaultAsync(s => s.IdSanPham == id && s.TrangThai == true);

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

            // Lấy khuyến mãi cho sản phẩm
            var khuyenMai = await PromotionHelper.GetBestPromotionForProduct(_context, id, sanPham.IdDanhMuc);
            ViewBag.KhuyenMai = khuyenMai;
            
            Console.WriteLine($"[ChiTiet] Product ID: {id}, Promotion: {khuyenMai?.TenKhuyenMai ?? "None"}");
            
            if (khuyenMai != null && sanPham.BienTheSanPhams.Any())
            {
                var giaGoc = sanPham.BienTheSanPhams.Min(bt => bt.GiaBan) ?? 0;
                var giaKhuyenMai = PromotionHelper.CalculatePromotionPrice(giaGoc, khuyenMai);
                var phanTramGiam = PromotionHelper.CalculateDiscountPercentage(giaGoc, giaKhuyenMai);
                
                Console.WriteLine($"[ChiTiet] Original: {giaGoc}, Discounted: {giaKhuyenMai}, Percentage: {phanTramGiam}%");
                
                ViewBag.GiaKhuyenMai = giaKhuyenMai;
                ViewBag.PhanTramGiam = phanTramGiam;
            }

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

            // Lấy tối đa 12 sản phẩm gợi ý (để có thể phân trang)
            var sanPhamGoiY = await _context.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                .Include(s => s.BienTheSanPhams)
                .Where(s => s.IdDanhMuc == sanPham.IdDanhMuc &&
                           s.IdSanPham != id &&
                           s.TrangThai == true)
                .ToListAsync();

            var sanPhamGoiYViewModel = sanPhamGoiY.Select(sp => new ChiTietSanPhamViewModel
            {
                Id = sp.IdSanPham,
                TenSanPham = sp.TenSanPham ?? "",
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
            ViewBag.ProductId = id;

            // Lấy userId từ đăng nhập
            int? userId = null;
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (claim != null && int.TryParse(claim.Value, out int uid))
                {
                    userId = uid;
                }
            }

            // Sắp xếp reviews: bình luận của user hiện tại lên đầu, sau đó là các bình luận khác theo thứ tự mới nhất
            if (userId.HasValue)
            {
                danhGias = danhGias
                    .OrderByDescending(d => d.IdTaiKhoan == userId.Value) // User's review first
                    .ThenByDescending(d => d.NgayDanhGia) // Then by newest
                    .ToList();
            }

            bool daMuaSanPham = false;
            bool daDanhGiaLanMuaCuoi = false;

            if (userId.HasValue)
            {
                // Lấy các đơn hàng đã hoàn thành của user
                var donHangIds = _context.DonHangs
                    .Where(dh => dh.IdTaiKhoan == userId.Value && dh.TrangThai == "hoàn thành")
                    .Select(dh => dh.IdDonHang)
                    .ToList();

                    // Lấy các chi tiết đơn hàng của các đơn đã hoàn thành
                    var chiTietDonHangs = _context.ChiTietDonHangs
                        .Where(ct => ct.IdDonHang.HasValue && donHangIds.Contains(ct.IdDonHang.Value))
                        .ToList();

                    // Lấy các IdBienThe của sản phẩm cần kiểm tra
                    var bienTheIds = _context.BienTheSanPhams
                        .Where(bt => bt.IdSanPham == id)
                        .Select(bt => bt.IdBienThe)
                        .ToList();

                    // Lấy lần mua cuối cùng của sản phẩm này
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
                        
                        // Kiểm tra xem đã đánh giá cho lần mua cuối chưa
                        if (ngayMuaCuoi != null)
                        {
                            daDanhGiaLanMuaCuoi = _context.DanhGia
                                .Any(dg => dg.IdTaiKhoan == userId.Value && 
                                          dg.IdSanPham == id && 
                                          dg.NgayDanhGia >= ngayMuaCuoi);
                        }
                    }
                }

                ViewBag.DaMuaSanPham = daMuaSanPham;
                ViewBag.DaDanhGiaLanMuaCuoi = daDanhGiaLanMuaCuoi;

            var model = new ChiTietSanPhamViewModel
            {
                Id = sanPham.IdSanPham,
                IdDanhMuc = sanPham.IdDanhMuc ?? 0,
                TenSanPham = sanPham.TenSanPham ?? "",
                MoTa = sanPham.MoTa ?? "",
                GiaBan = sanPham.BienTheSanPhams.Any() ? sanPham.BienTheSanPhams.Min(bt => bt.GiaBan) : null,
                MaxPrice = sanPham.BienTheSanPhams.Any() ? sanPham.BienTheSanPhams.Max(bt => bt.GiaBan) : null,
                TrangThai = sanPham.TrangThai,
                CachSuDung = sanPham.CachSuDung ?? "",
                NgayTao = sanPham.NgayTao,
                TenDanhMuc = sanPham.IdDanhMucNavigation?.TenDanhMuc ?? "",
                //DuongDanSEO = sanPham.IdDanhMucNavigation?.DuongDanSeo ?? "",
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

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ThemVaoGioHang(int productId, int variantId, int quantity)
        {
            try
            {
                // Kiểm tra người dùng đã đăng nhập
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });
                }

                BienTheSanPham? bienThe = null;
                SanPham? sanPham = null;

                if (variantId > 0)
                {
                    // Sản phẩm có biến thể
                    bienThe = await _context.BienTheSanPhams
                        .Include(bt => bt.IdSanPhamNavigation)
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == variantId && bt.IdSanPham == productId);

                    if (bienThe == null)
                    {
                        return Json(new { success = false, message = "Biến thể sản phẩm không tồn tại" });
                    }

                    if (bienThe.SoLuongTonKho < quantity)
                    {
                        return Json(new { success = false, message = "Số lượng vượt quá tồn kho" });
                    }
                }
                else
                {
                    // Sản phẩm không có biến thể
                    sanPham = await _context.SanPhams
                        .FirstOrDefaultAsync(sp => sp.IdSanPham == productId);

                    if (sanPham == null)
                    {
                        return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                    }

                    // Tạo một biến thể ảo cho sản phẩm không có biến thể
                    bienThe = new BienTheSanPham
                    {
                        IdSanPham = productId,
                        IdSanPhamNavigation = sanPham,
                        GiaBan = 0,
                        SoLuongTonKho = 999, // Giả sử luôn có hàng cho sản phẩm không có biến thể
                        Sku = $"SP{productId}"
                    };

                    // Lưu biến thể ảo vào database nếu chưa có
                    var existingVariant = await _context.BienTheSanPhams
                        .FirstOrDefaultAsync(bt => bt.IdSanPham == productId && (bt.Sku == $"SP{productId}" || bt.IdGiaTris == null || !bt.IdGiaTris.Any()));

                    if (existingVariant == null)
                    {
                        _context.BienTheSanPhams.Add(bienThe);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        bienThe = existingVariant;
                        variantId = existingVariant.IdBienThe;
                    }
                }

                // Tìm hoặc tạo giỏ hàng cho user
                var gioHang = await _context.GioHangs
                    .FirstOrDefaultAsync(gh => gh.IdTaiKhoan == userId);

                if (gioHang == null)
                {
                    gioHang = new GioHang
                    {
                        IdTaiKhoan = userId,
                        NgayCapNhat = DateTime.Now
                    };
                    _context.GioHangs.Add(gioHang);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                var chiTietGioHang = await _context.ChiTietGioHangs
                    .FirstOrDefaultAsync(ct => ct.IdGioHang == gioHang.IdGioHang && ct.IdBienThe == (variantId > 0 ? variantId : bienThe.IdBienThe));

                if (chiTietGioHang != null)
                {
                    // Nếu đã có, cập nhật số lượng
                    var soLuongMoi = chiTietGioHang.SoLuong + quantity;
                    if (soLuongMoi > bienThe.SoLuongTonKho)
                    {
                        return Json(new { success = false, message = "Số lượng vượt quá tồn kho" });
                    }
                    chiTietGioHang.SoLuong = soLuongMoi;
                }
                else
                {
                    // Nếu chưa có, thêm mới
                    chiTietGioHang = new ChiTietGioHang
                    {
                        IdGioHang = gioHang.IdGioHang,
                        IdBienThe = variantId > 0 ? variantId : bienThe.IdBienThe,
                        SoLuong = quantity
                    };
                    _context.ChiTietGioHangs.Add(chiTietGioHang);
                }

                // Cập nhật thời gian giỏ hàng
                gioHang.NgayCapNhat = DateTime.Now;
                
                await _context.SaveChangesAsync();

                // Lấy số lượng sản phẩm trong giỏ hàng để trả về
                var cartCount = await _context.ChiTietGioHangs
                    .Where(ct => ct.IdGioHang == gioHang.IdGioHang)
                    .CountAsync();

                return Json(new { success = true, message = "Đã thêm vào giỏ hàng thành công", cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MuaNgay(int productId, int variantId, int quantity)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"MuaNgay called with productId: {productId}, variantId: {variantId}, quantity: {quantity}");
                
                BienTheSanPham? bienThe = null;
                SanPham? sanPham = null;

                if (variantId > 0)
                {
                    // Sản phẩm có biến thể
                    bienThe = await _context.BienTheSanPhams
                        .Include(bt => bt.IdSanPhamNavigation)
                            .ThenInclude(sp => sp!.AnhSanPhams)
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == variantId && bt.IdSanPham == productId);

                    if (bienThe == null)
                    {
                        Console.WriteLine($"Biến thể {variantId} của sản phẩm {productId} không tồn tại");
                        TempData["Error"] = "Biến thể sản phẩm không tồn tại";
                        return RedirectToAction("Index", new { id = productId });
                    }

                    // Kiểm tra số lượng tồn kho
                    if (quantity > (bienThe.SoLuongTonKho ?? 0))
                    {
                        Console.WriteLine($"Số lượng {quantity} vượt quá tồn kho {bienThe.SoLuongTonKho}");
                        TempData["Error"] = "Số lượng vượt quá tồn kho";
                        return RedirectToAction("Index", new { id = productId });
                    }
                }
                else
                {
                    // Sản phẩm không có biến thể
                    sanPham = await _context.SanPhams
                        .Include(sp => sp.AnhSanPhams)
                        .FirstOrDefaultAsync(sp => sp.IdSanPham == productId);

                    if (sanPham == null)
                    {
                        Console.WriteLine($"Sản phẩm {productId} không tồn tại");
                        TempData["Error"] = "Sản phẩm không tồn tại";
                        return RedirectToAction("Index", new { id = productId });
                    }

                    // Tạo biến thể ảo cho sản phẩm không có biến thể
                    bienThe = new BienTheSanPham
                    {
                        IdSanPham = productId,
                        IdSanPhamNavigation = sanPham,
                        GiaBan = 0,
                        SoLuongTonKho = 999, // Giả sử luôn có hàng
                        Sku = $"SP{productId}"
                    };
                }

                // Lấy ảnh chính của sản phẩm
                var linkAnh = GetProductMainImage(bienThe.IdSanPhamNavigation?.AnhSanPhams);

                // Lấy giá gốc
                var giaGoc = bienThe.GiaBan ?? 0;
                
                // Tính giá khuyến mãi nếu có
                var khuyenMai = await PromotionHelper.GetBestPromotionForProduct(_context, productId, bienThe.IdSanPhamNavigation?.IdDanhMuc);
                var giaBan = giaGoc;
                
                if (khuyenMai != null)
                {
                    giaBan = PromotionHelper.CalculatePromotionPrice(giaGoc, khuyenMai);
                    Console.WriteLine($"Áp dụng khuyến mãi: Giá gốc = {giaGoc}, Giá sau KM = {giaBan}");
                }

                // Tạo thông tin sản phẩm để chuyển đến trang thanh toán
                var buyNowItem = new
                {
                    idBienThe = variantId > 0 ? bienThe.IdBienThe : 0, // Dùng 0 cho sản phẩm không có biến thể
                    name = bienThe.IdSanPhamNavigation?.TenSanPham ?? "",
                    price = giaBan, // Sử dụng giá sau khuyến mãi
                    quantity = quantity,
                    total = giaBan * quantity,
                    linkAnh = linkAnh
                };

                Console.WriteLine($"Created buyNowItem: {JsonSerializer.Serialize(buyNowItem)}");

                // Lưu thông tin vào TempData để chuyển sang trang thanh toán
                TempData["BuyNowItem"] = JsonSerializer.Serialize(buyNowItem);
                
                Console.WriteLine("Redirecting to Pay/Index");
                // Chuyển trực tiếp đến trang thanh toán
                return RedirectToAction("Index", "Pay", new { area = "KhachHang" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MuaNgay: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index", new { id = productId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ThemDanhGia(int productId, int rating, string comment, List<IFormFile> images)
        {
            try
            {
                Console.WriteLine($"ThemDanhGia called - productId: {productId}, rating: {rating}, comment length: {comment?.Length ?? 0}, images count: {images?.Count ?? 0}");

                if (rating < 1 || rating > 5)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn số sao để đánh giá sản phẩm.";
                    return RedirectToAction("Index", new { id = productId });
                }

                var sanPham = await _context.SanPhams.FindAsync(productId);
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction("Index", new { id = productId });
                }

                var danhGia = new DanhGium
                {
                    IdSanPham = productId,
                    IdTaiKhoan = 1,
                    SoSao = rating,
                    BinhLuan = comment?.Trim(),
                    NgayDanhGia = DateTime.Now
                };

                if (images != null && images.Count > 0)
                {
                    var imageUrls = new List<string>();
                    var maxImages = Math.Min(images.Count, 5);
                    bool hasImageError = false;
                    string imageErrorMsg = "";
                    for (int i = 0; i < maxImages; i++)
                    {
                        var image = images[i];
                        try
                        {
                            if (image.Length > 0)
                            {
                                var fileName = $"review_{productId}_{DateTime.Now.Ticks}_{i}{Path.GetExtension(image.FileName)}";
                                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "reviews");
                                Directory.CreateDirectory(uploadsPath);
                                var filePath = Path.Combine(uploadsPath, fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }
                                imageUrls.Add($"/images/reviews/{fileName}");
                            }
                            else
                            {
                                hasImageError = true;
                                imageErrorMsg += $"Ảnh thứ {i+1} bị lỗi hoặc rỗng. ";
                            }
                        }
                        catch (Exception imgEx)
                        {
                            hasImageError = true;
                            imageErrorMsg += $"Lỗi upload ảnh thứ {i+1}: {imgEx.Message} ";
                        }
                    }
                    if (hasImageError)
                    {
                        TempData["ErrorMessage"] = $"Có lỗi khi upload ảnh: {imageErrorMsg}";
                        return RedirectToAction("Index", new { id = productId });
                    }
                    if (imageUrls.Count > 0)
                    {
                        danhGia.AnhDanhGia = string.Join(";", imageUrls);
                    }
                }

                _context.DanhGia.Add(danhGia);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
                return RedirectToAction("CamOnDanhGia", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index", new { id = productId });
            }
        }

        public async Task<IActionResult> CamOnDanhGia(int productId)
        {
            // Sử dụng DbSet đúng tên (SanPhams)
            var sanPham = await _context.SanPhams.FindAsync(productId);
            ViewBag.ProductName = sanPham?.TenSanPham ?? "Sản phẩm";
            ViewBag.ProductId = productId;
            return View();
        }

        public async Task<IActionResult> LaySanPhamGoiY(int productId)
        {
            try
            {
                // Sử dụng DbSet đúng tên
                var sanPham = await _context.SanPhams.FindAsync(productId);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var sanPhamGoiY = await _context.SanPhams
                    .Include(s => s.AnhSanPhams)
                    .Where(s => s.IdDanhMuc == sanPham.IdDanhMuc &&
                               s.IdSanPham != productId &&
                               s.TrangThai == true)
                    .Take(4)
                    .Select(s => new
                    {
                        Id = s.IdSanPham,
                        Name = s.TenSanPham,
                        Price = s.BienTheSanPhams.Any() ? s.BienTheSanPhams.Min(bt => bt.GiaBan) : (decimal?)null,
                        AnhChinh = s.AnhSanPhams.Where(a => a.LoaiAnh == "chinh").Select(a => a.DuongDan).FirstOrDefault(),
                        DiemDanhGia = 0,
                        SoLuongDanhGia = 0
                    })
                    .ToListAsync();

                return Json(new { success = true, products = sanPhamGoiY });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)] // 100MB
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100MB
        public async Task<IActionResult> GuiDanhGia(int productId, int rating, string comment, List<IFormFile> images, int? editReviewId)
        {
            Console.WriteLine($"[DEBUG] === GuiDanhGia ACTION CALLED ===");
            Console.WriteLine($"[DEBUG] ProductId: {productId}, Rating: {rating}, EditReviewId: {editReviewId}");
            Console.WriteLine($"[DEBUG] Comment length: {comment?.Length ?? 0}");
            Console.WriteLine($"[DEBUG] Images count: {images?.Count ?? 0}");
            
            try
            {
                // Kiểm tra người dùng đã đăng nhập
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để đánh giá";
                    return RedirectToAction("Index", new { id = productId });
                }

                // Kiểm tra sản phẩm tồn tại
                var sanPham = await _context.SanPhams.FindAsync(productId);
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "SanPham");
                }

                // Kiểm tra đã mua sản phẩm chưa
                var donHangIds = await _context.DonHangs
                    .Where(dh => dh.IdTaiKhoan == userId && dh.TrangThai == "hoàn thành")
                    .Select(dh => dh.IdDonHang)
                    .ToListAsync();

                var bienTheIds = await _context.BienTheSanPhams
                    .Where(bt => bt.IdSanPham == productId)
                    .Select(bt => bt.IdBienThe)
                    .ToListAsync();

                var daMua = await _context.ChiTietDonHangs
                    .AnyAsync(ct => ct.IdDonHang.HasValue && 
                                   donHangIds.Contains(ct.IdDonHang.Value) && 
                                   ct.IdBienThe.HasValue && 
                                   bienTheIds.Contains(ct.IdBienThe.Value));

                if (!daMua)
                {
                    TempData["ErrorMessage"] = "Bạn cần mua sản phẩm này để có thể đánh giá";
                    return RedirectToAction("Index", new { id = productId });
                }

                // Kiểm tra đã đánh giá lần mua cuối chưa
                var lanMuaCuoi = await _context.ChiTietDonHangs
                    .Where(ct => ct.IdDonHang.HasValue && 
                                donHangIds.Contains(ct.IdDonHang.Value) && 
                                ct.IdBienThe.HasValue && 
                                bienTheIds.Contains(ct.IdBienThe.Value))
                    .OrderByDescending(ct => ct.IdDonHang)
                    .FirstOrDefaultAsync();

                if (lanMuaCuoi != null && lanMuaCuoi.IdDonHang.HasValue)
                {
                    var ngayMuaCuoi = await _context.DonHangs
                        .Where(dh => dh.IdDonHang == lanMuaCuoi.IdDonHang.Value)
                        .Select(dh => dh.NgayDat)
                        .FirstOrDefaultAsync();

                    // Nếu không phải edit, kiểm tra đã đánh giá chưa
                    if (editReviewId == null)
                    {
                        var daDanhGia = await _context.DanhGia
                            .AnyAsync(dg => dg.IdTaiKhoan == userId && 
                                           dg.IdSanPham == productId && 
                                           ngayMuaCuoi != null && 
                                           dg.NgayDanhGia >= ngayMuaCuoi);

                        if (daDanhGia)
                        {
                            TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi";
                            return RedirectToAction("Index", new { id = productId });
                        }
                    }
                }

                // Xử lý upload hình ảnh (tối đa 5)
                var imagePaths = new List<string>();
                if (images != null && images.Count > 0)
                {
                    Console.WriteLine($"[DEBUG] Số ảnh nhận được: {images.Count}");
                    
                    // Giới hạn tối đa 5 ảnh
                    var imagesToUpload = images.Take(5).ToList();

                    foreach (var image in imagesToUpload)
                    {
                        Console.WriteLine($"[DEBUG] Đang xử lý: {image.FileName}, Size: {image.Length} bytes");
                        if (image.Length > 0)
                        {
                            // Tạo tên file duy nhất
                            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "image_danhgia");
                            
                            // Tạo thư mục nếu chưa tồn tại
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var filePath = Path.Combine(uploadsFolder, fileName);

                            // Lưu file
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            // Thêm đường dẫn vào list (lưu đường dẫn tương đối)
                            imagePaths.Add($"/Images/image_danhgia/{fileName}");
                        }
                    }
                }

                // Kiểm tra nếu là edit hay tạo mới
                if (editReviewId.HasValue)
                {
                    // Cập nhật đánh giá cũ
                    var existingReview = await _context.DanhGia
                        .FirstOrDefaultAsync(dg => dg.IdDanhGia == editReviewId.Value && dg.IdTaiKhoan == userId);
                    
                    if (existingReview != null)
                    {
                        existingReview.SoSao = rating;
                        existingReview.BinhLuan = comment;
                        
                        // Nếu có ảnh mới, cập nhật ảnh
                        if (imagePaths.Count > 0)
                        {
                            existingReview.AnhDanhGia = string.Join(",", imagePaths);
                        }
                        // Nếu không có ảnh mới, giữ nguyên ảnh cũ (không làm gì)
                        
                        existingReview.NgayDanhGia = DateTime.Now; // Cập nhật thời gian sửa
                        
                        _context.DanhGia.Update(existingReview);
                        await _context.SaveChangesAsync();
                        
                        TempData["SuccessMessage"] = "Đã cập nhật đánh giá thành công";
                        return RedirectToAction("Index", new { id = productId });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy đánh giá để cập nhật";
                        return RedirectToAction("Index", new { id = productId });
                    }
                }

                // Tạo đánh giá mới
                var danhGia = new DanhGium
                {
                    IdTaiKhoan = userId,
                    IdSanPham = productId,
                    SoSao = rating,
                    BinhLuan = comment,
                    AnhDanhGia = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null,
                    NgayDanhGia = DateTime.Now
                };

                Console.WriteLine($"[DEBUG] Đã upload {imagePaths.Count} ảnh");
                
                _context.DanhGia.Add(danhGia);
                await _context.SaveChangesAsync();

                Console.WriteLine("[DEBUG] Đã lưu đánh giá vào database");

                // Lưu tên sản phẩm vào ViewBag để hiển thị trong trang cảm ơn
                ViewBag.ProductName = sanPham.TenSanPham;
                ViewBag.ProductId = productId;

                Console.WriteLine("[DEBUG] Chuẩn bị return View CamOnDanhGia");
                
                // Chuyển đến trang cảm ơn
                return View("CamOnDanhGia");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in GuiDanhGia: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }
                
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi đánh giá: " + ex.Message;
                return RedirectToAction("Index", new { id = productId });
            }
        }
    }
}
