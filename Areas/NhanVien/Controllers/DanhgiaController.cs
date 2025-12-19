using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Final_VS1.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "Nhanvien,admin")]
    [Route("nhanvien/reviews")]
    public class DanhgiaController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DanhgiaController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm, int? filterStar, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.DanhGia
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdSanPhamNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                query = query.Where(d =>
                    (d.BinhLuan ?? string.Empty).Contains(keyword) ||
                    (d.IdTaiKhoanNavigation != null && (d.IdTaiKhoanNavigation.HoTen ?? string.Empty).Contains(keyword)) ||
                    (d.IdSanPhamNavigation != null && (d.IdSanPhamNavigation.TenSanPham ?? string.Empty).Contains(keyword)));
            }

            if (filterStar.HasValue && filterStar.Value > 0)
            {
                query = query.Where(d => d.SoSao == filterStar.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var danhGia = await query
                .OrderByDescending(d => d.NgayDanhGia)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var reviewOrders = new Dictionary<int, (int Id, string Status, string Date)>();
            foreach (var review in danhGia)
            {
                var orderInfo = await _context.DonHangs
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                    .Where(d => d.IdTaiKhoan == review.IdTaiKhoan &&
                                d.ChiTietDonHangs.Any(ct => ct.IdBienTheNavigation != null &&
                                                            ct.IdBienTheNavigation.IdSanPham == review.IdSanPham))
                    .OrderByDescending(d => d.NgayDat)
                    .Select(d => new { d.IdDonHang, d.TrangThai, d.NgayDat })
                    .FirstOrDefaultAsync();

                if (orderInfo != null)
                {
                    reviewOrders[review.IdDanhGia] = (
                        orderInfo.IdDonHang,
                        orderInfo.TrangThai ?? "Không xác định",
                        orderInfo.NgayDat?.ToString("dd/MM/yyyy") ?? "N/A"
                    );
                }
            }

            ViewBag.TotalReviews = await _context.DanhGia.CountAsync();
            ViewBag.AverageRating = await _context.DanhGia
                .Where(d => d.SoSao.HasValue)
                .AverageAsync(d => (double?)d.SoSao) ?? 0;
            ViewBag.ReviewsWith5Stars = await _context.DanhGia.CountAsync(d => d.SoSao == 5);
            ViewBag.ReviewsWith4Stars = await _context.DanhGia.CountAsync(d => d.SoSao == 4);
            ViewBag.ReviewsWith3Stars = await _context.DanhGia.CountAsync(d => d.SoSao == 3);
            ViewBag.ReviewsWith2Stars = await _context.DanhGia.CountAsync(d => d.SoSao == 2);
            ViewBag.ReviewsWith1Star = await _context.DanhGia.CountAsync(d => d.SoSao == 1);
            ViewBag.ReviewsWithReply = await _context.DanhGia.CountAsync(d => !string.IsNullOrEmpty(d.TraLoiCuaShop));
            ViewBag.ReviewsWithoutReply = await _context.DanhGia.CountAsync(d => string.IsNullOrEmpty(d.TraLoiCuaShop));

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.FilterStar = filterStar;
            ViewBag.ReviewOrders = reviewOrders;

            return View(danhGia);
        }

        [HttpPost("replyreview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyReview(int idDanhGia, string traLoi)
        {
            if (string.IsNullOrWhiteSpace(traLoi))
            {
                return Json(new { success = false, message = "Nội dung trả lời không được để trống" });
            }

            var danhGia = await _context.DanhGia.FindAsync(idDanhGia);
            if (danhGia == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            danhGia.TraLoiCuaShop = traLoi;
            danhGia.NgayTraLoi = DateTime.Now;

            int? staffId = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var parsedId))
            {
                staffId = parsedId;
            }

            _context.LogHoatDongs.Add(new LogHoatDong
            {
                IdTaiKhoan = staffId,
                HanhDong = "Tra loi danh gia",
                DoiTuong = "DanhGia",
                IdDoiTuong = idDanhGia,
                ThoiGian = DateTime.Now,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã trả lời đánh giá thành công",
                ngayTraLoi = danhGia.NgayTraLoi?.ToString("dd/MM/yyyy HH:mm")
            });
        }

        [HttpPost("deletereview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var danhGia = await _context.DanhGia.FindAsync(id);
                if (danhGia == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                _context.DanhGia.Remove(danhGia);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa đánh giá thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost("deletemultiple")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có đánh giá nào được chọn" });
                }

                var danhGias = await _context.DanhGia.Where(d => ids.Contains(d.IdDanhGia)).ToListAsync();
                _context.DanhGia.RemoveRange(danhGias);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã xóa {danhGias.Count} đánh giá" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet("getreviewdetail")]
        public async Task<IActionResult> GetReviewDetail(int id)
        {
            try
            {
                var danhGia = await _context.DanhGia
                    .Include(d => d.IdTaiKhoanNavigation)
                    .Include(d => d.IdSanPhamNavigation)
                    .FirstOrDefaultAsync(d => d.IdDanhGia == id);

                if (danhGia == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                var orderInfo = await _context.DonHangs
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                    .Where(d => d.IdTaiKhoan == danhGia.IdTaiKhoan &&
                                d.ChiTietDonHangs.Any(ct => ct.IdBienTheNavigation != null &&
                                                            ct.IdBienTheNavigation.IdSanPham == danhGia.IdSanPham))
                    .OrderByDescending(d => d.NgayDat)
                    .Select(d => new
                    {
                        d.IdDonHang,
                        d.TrangThai,
                        d.NgayDat
                    })
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        idDanhGia = danhGia.IdDanhGia,
                        tenKhachHang = danhGia.IdTaiKhoanNavigation?.HoTen ?? "Khách ẩn danh",
                        tenSanPham = danhGia.IdSanPhamNavigation?.TenSanPham ?? "Không xác định",
                        soSao = danhGia.SoSao ?? 0,
                        binhLuan = danhGia.BinhLuan,
                        anhDanhGia = danhGia.AnhDanhGia,
                        ngayDanhGia = danhGia.NgayDanhGia?.ToString("dd/MM/yyyy HH:mm"),
                        traLoiCuaShop = danhGia.TraLoiCuaShop,
                        ngayTraLoi = danhGia.NgayTraLoi?.ToString("dd/MM/yyyy HH:mm"),
                        orderId = orderInfo?.IdDonHang,
                        orderStatus = orderInfo?.TrangThai,
                        orderDate = orderInfo?.NgayDat?.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
