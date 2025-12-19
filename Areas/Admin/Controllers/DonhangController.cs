using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Models;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using Final_VS1.Areas.Admin.Models;


namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class DonhangController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DonhangController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.DonHangs
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation!)
                        .ThenInclude(bt => bt.IdSanPhamNavigation!)
                .Include(d => d.TimelineDonHangs)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var order = await _context.DonHangs
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdDiaChiNavigation)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation!)
                        .ThenInclude(bt => bt.IdSanPhamNavigation!)
                .FirstOrDefaultAsync(d => d.IdDonHang == id);

            if (order == null)
            {
                return NotFound();
            }

            var orderDetail = new OrderDetailResponse
            {
                Id = order.IdDonHang,
                CustomerName = order.IdTaiKhoanNavigation?.HoTen ?? "Khách vãng lai",
                CustomerEmail = order.IdTaiKhoanNavigation?.Email ?? "N/A",
                CustomerPhone = order.IdDiaChiNavigation?.SoDienThoai ?? "N/A",
                Address = order.IdDiaChiNavigation?.DiaChiChiTiet ?? "Chưa cập nhật",
                OrderDate = order.NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                Status = order.TrangThai ?? "Không xác định",
                PaymentMethod = order.PhuongThucThanhToan ?? "Chưa xác định",
                TotalAmount = order.TongTien?.ToString("N0") + " ₫",
                Items = order.ChiTietDonHangs.Select(ct => new OrderItemResponse
                {
                    ProductName = ct.IdBienTheNavigation?.IdSanPhamNavigation?.TenSanPham ?? "Sản phẩm không xác định",
                    VariantSku = ct.IdBienTheNavigation?.Sku ?? "N/A",
                    Quantity = ct.SoLuong ?? 0,
                    Price = ct.GiaLucDat?.ToString("N0") + " ₫",
                    SubTotal = ((ct.SoLuong ?? 0) * (ct.GiaLucDat ?? 0)).ToString("N0") + " ₫"
                }).ToList()
            };

            return Json(orderDetail);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var allowedStatuses = new[] { "Chờ xác nhận", "Đã xác nhận", "Đang giao", "Hoàn thành" };

            if (string.IsNullOrWhiteSpace(status) || !allowedStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });
            }

            var order = await _context.DonHangs.FindAsync(id);
            if (order == null)
            {
                return Json(new { success = false });
            }

            var currentStatus = order.TrangThai ?? string.Empty;

            // Only allow forward progression: Chờ xác nhận -> Đã xác nhận -> Đang giao -> Hoàn thành
            var nextStatusMap = new Dictionary<string, string>
            {
                { string.Empty, "Chờ xác nhận" },
                { "Chờ xác nhận", "Đã xác nhận" },
                { "Đã xác nhận", "Đang giao" },
                { "Đang giao", "Hoàn thành" }
            };

            if (!nextStatusMap.TryGetValue(currentStatus, out var allowedNext) || status != allowedNext)
            {
                return Json(new { success = false, message = "Không được phép chuyển sang trạng thái này theo thứ tự quy định" });
            }

            // Lưu trạng thái cũ trước khi cập nhật
            var oldStatus = order.TrangThai;
            order.TrangThai = status;

            // Tạo bản ghi timeline mới
            var timeline = new TimelineDonHang
            {
                IdDonHang = id,
                TrangThaiMoi = status,
                GhiChu = $"Thay đổi từ '{oldStatus}' sang '{status}'",
                NgayCapNhat = DateTime.Now
            };

            _context.TimelineDonHangs.Add(timeline);
            await _context.SaveChangesAsync();

            return Json(new { success = true, updateTime = timeline.NgayCapNhat?.ToString("dd/MM/yyyy HH:mm") });
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeline(int id)
        {
            var timelines = await _context.TimelineDonHangs
                .Where(t => t.IdDonHang == id)
                .OrderByDescending(t => t.NgayCapNhat)
                .Select(t => new
                {
                    status = t.TrangThaiMoi,
                    note = t.GhiChu,
                    date = t.NgayCapNhat.HasValue ? t.NgayCapNhat.Value.ToString("dd/MM/yyyy HH:mm:ss") : "N/A"
                })
                .ToListAsync();

            return Json(timelines);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation!)
                        .ThenInclude(bt => bt.IdSanPhamNavigation!)
                .FirstOrDefaultAsync(d => d.IdDonHang == id);

            if (order == null)
            {
                return Json(new { success = false });
            }

            var orderDetails = order.ChiTietDonHangs.Select(ct => new
            {
                tenSanPham = ct.IdBienTheNavigation?.IdSanPhamNavigation?.TenSanPham ?? "N/A",
                soLuong = ct.SoLuong ?? 0,
                giaLucDat = ct.GiaLucDat ?? 0
            }).ToList();

            return Json(new { success = true, chiTietDonHangs = orderDetails });
        }

        // Trang chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.DonHangs
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdDiaChiNavigation)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation!)
                        .ThenInclude(bt => bt.IdSanPhamNavigation!)
                            .ThenInclude(sp => sp.AnhSanPhams)
                .Include(d => d.TimelineDonHangs)
                .Include(d => d.IdVcNavigation)
                .FirstOrDefaultAsync(d => d.IdDonHang == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
