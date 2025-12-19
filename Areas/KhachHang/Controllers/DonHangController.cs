using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Final_VS1.Areas.KhachHang.ViewModels;
using System.Security.Claims;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class DonHangController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly ILogger<DonHangController> _logger;

        public DonHangController(LittleFishBeautyContext context, ILogger<DonHangController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method để lấy ảnh chính của sản phẩm theo thứ tự ưu tiên
        private string GetProductMainImage(ICollection<AnhSanPham>? anhSanPhams)
        {
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
                        return anhChinh.LinkCloudinary;
                    if (!string.IsNullOrEmpty(anhChinh.DuongDan))
                        return anhChinh.DuongDan;
                }
                
                // Bước 2: Nếu không có ảnh chính, tìm ảnh phụ
                var anhPhu = anhSanPhams.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.LoaiAnh) &&
                    (a.LoaiAnh.Trim().ToLower() == "phu" || a.LoaiAnh.Trim().ToLower() == "phụ"));
                    
                if (anhPhu != null)
                {
                    // Ưu tiên LinkCloudinary, nếu không có thì dùng DuongDan
                    if (!string.IsNullOrEmpty(anhPhu.LinkCloudinary))
                        return anhPhu.LinkCloudinary;
                    if (!string.IsNullOrEmpty(anhPhu.DuongDan))
                        return anhPhu.DuongDan;
                }
                
                // Bước 3: Nếu không có ảnh chính/phụ, lấy ảnh đầu tiên có LinkCloudinary hoặc DuongDan
                var anyImage = anhSanPhams.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.LinkCloudinary) || !string.IsNullOrEmpty(a.DuongDan));
                
                if (anyImage != null)
                {
                    if (!string.IsNullOrEmpty(anyImage.LinkCloudinary))
                        return anyImage.LinkCloudinary;
                    if (!string.IsNullOrEmpty(anyImage.DuongDan))
                        return anyImage.DuongDan;
                }
            }
            
            // Nếu không có ảnh nào, trả về ảnh mặc định
            return "/images/noimage.jpg";
        }

        public async Task<IActionResult> Index(string? status)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var query = _context.DonHangs
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation!)
                            .ThenInclude(bt => bt.IdSanPhamNavigation!)
                                .ThenInclude(sp => sp.AnhSanPhams)
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation!)
                            .ThenInclude(bt => bt.IdGiaTris)
                                .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .Include(d => d.IdDiaChiNavigation)
                    .Include(d => d.IdTaiKhoanNavigation)
                    .Where(d => d.IdTaiKhoan == int.Parse(userId) 
                        && d.TrangThai != "Khởi tạo thanh toán"); // THAY ĐỔI: Ẩn đơn hàng đang khởi tạo

                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Đang giao")
                    {
                        // Gộp các trạng thái "Đang xử lý", "Đã xác nhận", "Đang giao" thành "Đang giao"
                        query = query.Where(d => d.TrangThai == "Đang xử lý" || d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang giao");
                    }
                    else
                    {
                        query = query.Where(d => d.TrangThai == status);
                    }
                }

                var donHangs = await query.OrderByDescending(d => d.NgayDat).ToListAsync();

                // Tạo dictionary để lưu ảnh sản phẩm theo IdSanPham
                var productImages = new Dictionary<int, string>();
                foreach (var donHang in donHangs)
                {
                    foreach (var chiTiet in donHang.ChiTietDonHangs)
                    {
                        var sanPham = chiTiet.IdBienTheNavigation?.IdSanPhamNavigation;
                        if (sanPham != null && !productImages.ContainsKey(sanPham.IdSanPham))
                        {
                            productImages[sanPham.IdSanPham] = GetProductMainImage(sanPham.AnhSanPhams);
                        }
                    }
                }
                ViewBag.ProductImages = productImages;

                var viewModel = new DonHangViewModel
                {
                    DonHangs = donHangs,
                    CurrentFilter = status,
                    TotalOrders = await _context.DonHangs.CountAsync(d => d.IdTaiKhoan == int.Parse(userId) && d.TrangThai != "Khởi tạo thanh toán"),
                    PendingOrders = await _context.DonHangs.CountAsync(d => d.IdTaiKhoan == int.Parse(userId) && d.TrangThai == "Chờ xác nhận"),
                    ShippingOrders = await _context.DonHangs.CountAsync(d => d.IdTaiKhoan == int.Parse(userId) && (d.TrangThai == "Đang xử lý" || d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đang giao")),
                    DeliveredOrders = await _context.DonHangs.CountAsync(d => d.IdTaiKhoan == int.Parse(userId) && d.TrangThai == "Hoàn thành"),
                    CancelledOrders = await _context.DonHangs.CountAsync(d => d.IdTaiKhoan == int.Parse(userId) && d.TrangThai == "Đã hủy")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn hàng";
                return View(new DonHangViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                }

                var donHang = await _context.DonHangs
                    .FirstOrDefaultAsync(d => d.IdDonHang == request.Id && d.IdTaiKhoan == int.Parse(userId));

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                if (donHang.TrangThai != "Chờ xác nhận")
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận" });
                }

                donHang.TrangThai = "Đã hủy";
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {request.Id} cancelled by user {userId}");
                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {request.Id}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng" });
            }
        }

        // Action để xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var donHang = await _context.DonHangs
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation!)
                            .ThenInclude(bt => bt.IdSanPhamNavigation!)
                                .ThenInclude(sp => sp.AnhSanPhams)
                    .Include(d => d.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation!)
                            .ThenInclude(bt => bt.IdGiaTris)
                                .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .Include(d => d.IdDiaChiNavigation)
                    .Include(d => d.IdTaiKhoanNavigation)
                    .Include(d => d.TimelineDonHangs.OrderBy(t => t.NgayCapNhat))
                    .FirstOrDefaultAsync(d => d.IdDonHang == id 
                        && d.IdTaiKhoan == int.Parse(userId) 
                        && d.TrangThai != "Khởi tạo thanh toán"); // THAY ĐỔI: Ngăn truy cập đơn đang khởi tạo

                if (donHang == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Index");
                }

                // Tạo dictionary để lưu ảnh sản phẩm
                var productImages = new Dictionary<int, string>();
                foreach (var chiTiet in donHang.ChiTietDonHangs)
                {
                    var sanPham = chiTiet.IdBienTheNavigation?.IdSanPhamNavigation;
                    if (sanPham != null && !productImages.ContainsKey(sanPham.IdSanPham))
                    {
                        productImages[sanPham.IdSanPham] = GetProductMainImage(sanPham.AnhSanPhams);
                    }
                }
                ViewBag.ProductImages = productImages;

                return View(donHang);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Details action for order {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết đơn hàng";
                return RedirectToAction("Index");
            }
        }

        // Action để hiển thị trang đặt hàng thành công
        // GET: /KhachHang/DonHang/DatHangThanhCong?orderCode=DHxxx
        public IActionResult DatHangThanhCong(string orderCode)
        {
            // Truyền mã đơn hàng sang View để hiển thị (nếu cần)
            ViewBag.OrderCode = orderCode;
            // Trả về View có tên DatHangThanhCong.cshtml
            return View();
        }

        // Action để xử lý return từ Stripe sau khi thanh toán
        // GET: /KhachHang/DonHang/StripePaymentReturn?payment_intent=pi_xxx&payment_intent_client_secret=xxx
        public async Task<IActionResult> StripePaymentReturn(string payment_intent, string payment_intent_client_secret)
        {
            if (string.IsNullOrEmpty(payment_intent))
            {
                // Nếu không có payment_intent, redirect về trang đơn hàng
                return RedirectToAction("Index");
            }

            Console.WriteLine($"[StripePaymentReturn] payment_intent={payment_intent}");

            // Chờ một chút để webhook có thể xử lý xong (webhook thường nhanh hơn redirect)
            await Task.Delay(1000);

            // Tìm đơn hàng theo StripePaymentIntentId trong bảng ThanhToan
            var thanhToan = await _context.ThanhToans
                .Include(tt => tt.IdDonHangNavigation)
                .FirstOrDefaultAsync(tt => tt.MaGiaoDichNganHang == payment_intent && tt.PhuongThuc == "Stripe");

            if (thanhToan != null && thanhToan.IdDonHangNavigation != null)
            {
                string orderCode = $"DH{thanhToan.IdDonHangNavigation.IdDonHang:D6}";
                Console.WriteLine($"[StripePaymentReturn] Found order: {orderCode}");
                return RedirectToAction("DatHangThanhCong", new { orderCode = orderCode });
            }
            else
            {
                // Nếu chưa tìm thấy (webhook chưa xử lý xong), đợi thêm
                Console.WriteLine("[StripePaymentReturn] Order not found yet, waiting 2s more...");
                await Task.Delay(2000);

                // Thử tìm lại
                thanhToan = await _context.ThanhToans
                    .Include(tt => tt.IdDonHangNavigation)
                    .FirstOrDefaultAsync(tt => tt.MaGiaoDichNganHang == payment_intent && tt.PhuongThuc == "Stripe");

                if (thanhToan != null && thanhToan.IdDonHangNavigation != null)
                {
                    string orderCode = $"DH{thanhToan.IdDonHangNavigation.IdDonHang:D6}";
                    Console.WriteLine($"[StripePaymentReturn] Found order after retry: {orderCode}");
                    return RedirectToAction("DatHangThanhCong", new { orderCode = orderCode });
                }
                else
                {
                    // Vẫn không tìm thấy - có thể webhook chưa chạy hoặc có lỗi
                    Console.WriteLine("[StripePaymentReturn] Order still not found - showing pending message");
                    ViewBag.OrderCode = "Đang xử lý...";
                    ViewBag.PendingMessage = "Đơn hàng của bạn đang được xử lý. Vui lòng kiểm tra trong danh sách đơn hàng sau vài giây.";
                    return View("DatHangThanhCong");
                }
            }
        }

        // ===============================================
        public class CancelOrderRequest
        {
            public int Id { get; set; }
        }
    }
}