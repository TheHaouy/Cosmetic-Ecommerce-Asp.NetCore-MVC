using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class ThanhToanController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public ThanhToanController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // GET: Admin/ThanhToan
        public async Task<IActionResult> Index(string status = "", string method = "", string q = "", string sort = "desc")
        {
            var query = _context.ThanhToans
                .Include(t => t.IdDonHangNavigation)
                    .ThenInclude(d => d.IdTaiKhoanNavigation)
                .Include(t => t.IdDonHangNavigation)
                    .ThenInclude(d => d.IdDiaChiNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                // Allow status filter like Success/Fail or exact db values
                query = query.Where(t => t.TrangThai.Contains(status));
            }

            if (!string.IsNullOrEmpty(method))
            {
                // Filter by payment method
                query = query.Where(t => t.PhuongThuc != null && t.PhuongThuc.Contains(method));
            }

            if (!string.IsNullOrEmpty(q))
            {
                // try parse an order id search
                int qid = 0;
                int.TryParse(q, out qid);

                query = query.Where(t =>
                    (t.MaGiaoDichNganHang != null && t.MaGiaoDichNganHang.Contains(q)) ||
                    (t.MaPhanHoi != null && t.MaPhanHoi.Contains(q)) ||
                    (t.PhuongThuc != null && t.PhuongThuc.Contains(q)) ||
                    (qid != 0 && t.IdDonHang == qid) ||
                    (t.IdDonHangNavigation != null && t.IdDonHangNavigation.IdTaiKhoanNavigation != null && t.IdDonHangNavigation.IdTaiKhoanNavigation.HoTen != null && t.IdDonHangNavigation.IdTaiKhoanNavigation.HoTen.Contains(q))
                );
            }

            // sort by payment time when available, otherwise by creation time
            if (string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase))
                query = query.OrderBy(t => t.ThoiGianThanhToan ?? t.NgayTao);
            else
                query = query.OrderByDescending(t => t.ThoiGianThanhToan ?? t.NgayTao);

            var list = await query.ToListAsync();
            ViewBag.FilterStatus = status;
            ViewBag.FilterMethod = method;
            ViewBag.SearchQuery = q;
            ViewBag.SortOrder = sort;
            
            // Load distinct statuses from DB so the view can build a filter list dynamically
            var statuses = await _context.ThanhToans
                .Where(t => !string.IsNullOrEmpty(t.TrangThai))
                .Select(t => t.TrangThai)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
            ViewBag.AvailableStatuses = statuses;
            
            // Load distinct payment methods from DB
            var methods = await _context.ThanhToans
                .Where(t => !string.IsNullOrEmpty(t.PhuongThuc))
                .Select(t => t.PhuongThuc)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            ViewBag.AvailableMethods = methods;
            
            return View(list);
        }

        // GET: Admin/ThanhToan/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var thanhToan = await _context.ThanhToans
                .Include(t => t.IdDonHangNavigation)
                    .ThenInclude(d => d.IdTaiKhoanNavigation)
                .Include(t => t.IdDonHangNavigation)
                    .ThenInclude(d => d.IdDiaChiNavigation)
                .FirstOrDefaultAsync(t => t.IdThanhToan == id);

            if (thanhToan == null)
                return NotFound();

            return View(thanhToan);
        }
    }
}
