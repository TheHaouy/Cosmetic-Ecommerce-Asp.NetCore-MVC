using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class KhuyenMaiController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public KhuyenMaiController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách khuyến mãi
        /// </summary>
        public async Task<IActionResult> Index(string? trangThai, string? search)
        {
            var query = _context.KhuyenMais
                .Include(k => k.KhuyenMaiSanPhams)
                .Include(k => k.KhuyenMaiDanhMucs)
                .Include(k => k.DieuKienKhuyenMais)
                .AsQueryable();

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(k => k.TrangThai == trangThai);
            }

            // Search theo tên
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(k => k.TenKhuyenMai.Contains(search));
            }

            var khuyenMais = await query
                .OrderByDescending(k => k.NgayTao)
                .ToListAsync();

            ViewBag.TrangThaiFilter = trangThai;
            ViewBag.SearchTerm = search;

            return View(khuyenMais);
        }

        /// <summary>
        /// Form tạo mới khuyến mãi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View("ThemSua", new KhuyenMai
            {
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(7),
                TrangThai = "NHAP",
                UuTien = 1
            });
        }

        /// <summary>
        /// Form chỉnh sửa khuyến mãi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var khuyenMai = await _context.KhuyenMais
                .Include(k => k.KhuyenMaiSanPhams)
                .Include(k => k.KhuyenMaiDanhMucs)
                .Include(k => k.DieuKienKhuyenMais)
                .FirstOrDefaultAsync(k => k.IdKhuyenMai == id);

            if (khuyenMai == null)
            {
                return NotFound();
            }

            await LoadDropdownData();
            return View("ThemSua", khuyenMai);
        }

        /// <summary>
        /// Lưu khuyến mãi mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai khuyenMai, int[]? selectedProducts, int[]? selectedCategories, string? gioBatDauStr, string? gioKetThucStr)
        {
            try
            {
                // Parse TimeSpan from string
                if (!string.IsNullOrEmpty(gioBatDauStr) && TimeSpan.TryParse(gioBatDauStr, out var gioBatDau))
                {
                    khuyenMai.GioBatDau = gioBatDau;
                }
                
                if (!string.IsNullOrEmpty(gioKetThucStr) && TimeSpan.TryParse(gioKetThucStr, out var gioKetThuc))
                {
                    khuyenMai.GioKetThuc = gioKetThuc;
                }

                // Validate
                if (khuyenMai.NgayKetThuc <= khuyenMai.NgayBatDau)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu!" });
                }

                // Lấy user ID hiện tại
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userId, out int currentUserId))
                {
                    khuyenMai.NguoiTao = currentUserId;
                }

                khuyenMai.NgayTao = DateTime.Now;
                khuyenMai.SoLuongDaBan = 0;

                _context.KhuyenMais.Add(khuyenMai);
                await _context.SaveChangesAsync();

                // Liên kết với sản phẩm
                if (selectedProducts != null && selectedProducts.Any())
                {
                    foreach (var productId in selectedProducts)
                    {
                        var product = await _context.SanPhams
                            .Include(s => s.BienTheSanPhams)
                            .FirstOrDefaultAsync(s => s.IdSanPham == productId);

                        if (product != null)
                        {
                            // Tính giá khuyến mãi
                            var giaGoc = product.BienTheSanPhams.Any() 
                                ? product.BienTheSanPhams.Min(bt => bt.GiaBan) ?? 0
                                : 0;

                            var giaKhuyenMai = TinhGiaKhuyenMai(giaGoc, khuyenMai);

                            _context.KhuyenMaiSanPhams.Add(new KhuyenMaiSanPham
                            {
                                IdKhuyenMai = khuyenMai.IdKhuyenMai,
                                IdSanPham = productId,
                                GiaKhuyenMai = giaKhuyenMai,
                                SoLuongConLai = khuyenMai.SoLuongGioiHan
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Liên kết với danh mục
                if (selectedCategories != null && selectedCategories.Any())
                {
                    foreach (var categoryId in selectedCategories)
                    {
                        _context.KhuyenMaiDanhMucs.Add(new KhuyenMaiDanhMuc
                        {
                            IdKhuyenMai = khuyenMai.IdKhuyenMai,
                            IdDanhMuc = categoryId,
                            ApDungChoSanPhamCon = true
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Tạo khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cập nhật khuyến mãi
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(KhuyenMai khuyenMai, int[]? selectedProducts, int[]? selectedCategories, string? gioBatDauStr, string? gioKetThucStr)
        {
            try
            {
                var existing = await _context.KhuyenMais
                    .Include(k => k.KhuyenMaiSanPhams)
                    .Include(k => k.KhuyenMaiDanhMucs)
                    .FirstOrDefaultAsync(k => k.IdKhuyenMai == khuyenMai.IdKhuyenMai);

                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
                }

                // Parse TimeSpan from string
                if (!string.IsNullOrEmpty(gioBatDauStr) && TimeSpan.TryParse(gioBatDauStr, out var gioBatDau))
                {
                    khuyenMai.GioBatDau = gioBatDau;
                }
                else
                {
                    khuyenMai.GioBatDau = null;
                }
                
                if (!string.IsNullOrEmpty(gioKetThucStr) && TimeSpan.TryParse(gioKetThucStr, out var gioKetThuc))
                {
                    khuyenMai.GioKetThuc = gioKetThuc;
                }
                else
                {
                    khuyenMai.GioKetThuc = null;
                }

                // Validate
                if (khuyenMai.NgayKetThuc <= khuyenMai.NgayBatDau)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu!" });
                }

                // Update fields
                existing.TenKhuyenMai = khuyenMai.TenKhuyenMai;
                existing.MoTa = khuyenMai.MoTa;
                existing.LoaiKhuyenMai = khuyenMai.LoaiKhuyenMai;
                existing.HinhThucGiam = khuyenMai.HinhThucGiam;
                existing.GiaTriGiam = khuyenMai.GiaTriGiam;
                existing.GiaTriGiamToiDa = khuyenMai.GiaTriGiamToiDa;
                existing.NgayBatDau = khuyenMai.NgayBatDau;
                existing.NgayKetThuc = khuyenMai.NgayKetThuc;
                existing.GioBatDau = khuyenMai.GioBatDau;
                existing.GioKetThuc = khuyenMai.GioKetThuc;
                existing.TrangThai = khuyenMai.TrangThai;
                existing.UuTien = khuyenMai.UuTien;
                existing.CoTheKetHop = khuyenMai.CoTheKetHop;
                existing.HienThiTrangChu = khuyenMai.HienThiTrangChu;
                existing.AnhBanner = khuyenMai.AnhBanner;
                existing.SoLuongGioiHan = khuyenMai.SoLuongGioiHan;

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userId, out int currentUserId))
                {
                    existing.NguoiSua = currentUserId;
                }
                existing.NgaySua = DateTime.Now;

                // Cập nhật sản phẩm
                _context.KhuyenMaiSanPhams.RemoveRange(existing.KhuyenMaiSanPhams);
                if (selectedProducts != null && selectedProducts.Any())
                {
                    foreach (var productId in selectedProducts)
                    {
                        var product = await _context.SanPhams
                            .Include(s => s.BienTheSanPhams)
                            .FirstOrDefaultAsync(s => s.IdSanPham == productId);

                        if (product != null)
                        {
                            var giaGoc = product.BienTheSanPhams.Any()
                                ? product.BienTheSanPhams.Min(bt => bt.GiaBan) ?? 0
                                : 0;

                            var giaKhuyenMai = TinhGiaKhuyenMai(giaGoc, khuyenMai);

                            _context.KhuyenMaiSanPhams.Add(new KhuyenMaiSanPham
                            {
                                IdKhuyenMai = khuyenMai.IdKhuyenMai,
                                IdSanPham = productId,
                                GiaKhuyenMai = giaKhuyenMai,
                                SoLuongConLai = khuyenMai.SoLuongGioiHan
                            });
                        }
                    }
                }

                // Cập nhật danh mục
                _context.KhuyenMaiDanhMucs.RemoveRange(existing.KhuyenMaiDanhMucs);
                if (selectedCategories != null && selectedCategories.Any())
                {
                    foreach (var categoryId in selectedCategories)
                    {
                        _context.KhuyenMaiDanhMucs.Add(new KhuyenMaiDanhMuc
                        {
                            IdKhuyenMai = khuyenMai.IdKhuyenMai,
                            IdDanhMuc = categoryId,
                            ApDungChoSanPhamCon = true
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa khuyến mãi
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var khuyenMai = await _context.KhuyenMais.FindAsync(id);
                if (khuyenMai == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
                }

                // Kiểm tra đã có người dùng chưa
                if (khuyenMai.SoLuongDaBan > 0)
                {
                    return Json(new { success = false, message = "Không thể xóa khuyến mãi đã được sử dụng!" });
                }

                _context.KhuyenMais.Remove(khuyenMai);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Thay đổi trạng thái khuyến mãi
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            try
            {
                var khuyenMai = await _context.KhuyenMais.FindAsync(id);
                if (khuyenMai == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
                }

                khuyenMai.TrangThai = status;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Tính giá sau khuyến mãi
        /// </summary>
        private decimal TinhGiaKhuyenMai(decimal giaGoc, KhuyenMai khuyenMai)
        {
            if (giaGoc <= 0) return 0;

            decimal giaKhuyenMai = giaGoc;

            switch (khuyenMai.HinhThucGiam)
            {
                case "PHAN_TRAM":
                    var giamGia = giaGoc * khuyenMai.GiaTriGiam / 100;
                    if (khuyenMai.GiaTriGiamToiDa.HasValue && giamGia > khuyenMai.GiaTriGiamToiDa.Value)
                    {
                        giamGia = khuyenMai.GiaTriGiamToiDa.Value;
                    }
                    giaKhuyenMai = giaGoc - giamGia;
                    break;

                case "SO_TIEN":
                    giaKhuyenMai = giaGoc - khuyenMai.GiaTriGiam;
                    break;

                case "GIA_CO_DINH":
                    giaKhuyenMai = khuyenMai.GiaTriGiam;
                    break;
            }

            return giaKhuyenMai > 0 ? giaKhuyenMai : 0;
        }

        /// <summary>
        /// Load data cho dropdown
        /// </summary>
        private async Task LoadDropdownData()
        {
            ViewBag.SanPhams = await _context.SanPhams
                .Where(s => s.TrangThai == true)
                .OrderBy(s => s.TenSanPham)
                .ToListAsync();

            ViewBag.DanhMucs = await _context.DanhMucs
                .OrderBy(d => d.TenDanhMuc)
                .ToListAsync();
        }

        #endregion
    }
}
