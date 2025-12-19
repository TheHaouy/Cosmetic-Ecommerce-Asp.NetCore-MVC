using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Final_VS1.Areas.KhachHang.Services; // Service VNPAY & Email
using Final_VS1.Data; // DbContext
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory; // <-- THÊM CHO MEMORY CACHE

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    public class VnpayController : Controller
    {
        private readonly VnpayService _vnpayService;
        private readonly IConfiguration _config;
        private readonly LittleFishBeautyContext _context;
        private readonly IOrderEmailService _orderEmailService;
        private readonly IMemoryCache _memoryCache; // <-- THÊM

        public VnpayController(VnpayService vnpayService, IConfiguration config, LittleFishBeautyContext context, IOrderEmailService orderEmailService, IMemoryCache memoryCache)
        {
            _vnpayService = vnpayService;
            _config = config;
            _context = context;
            _orderEmailService = orderEmailService;
            _memoryCache = memoryCache; // <-- THÊM
        }

        // Action này là nơi VNPAY trả trình duyệt của khách hàng về
        // URL: /KhachHang/Vnpay/PaymentReturn
        [AllowAnonymous] // Cho phép truy cập mà không cần đăng nhập
        public async Task<IActionResult> PaymentReturn()
        {
            // Lấy HashSecret từ config (xử lý null)
            string vnp_HashSecret = _config["Vnpay:HashSecret"] ?? "";

            var vnp_Params = HttpContext.Request.Query;
            var queryString = HttpContext.Request.QueryString.Value ?? ""; // Xử lý null

            if (string.IsNullOrEmpty(queryString) || vnp_Params.Count == 0)
            {
                // Chuyển đến trang lỗi với tham số area
                return RedirectToAction("ThanhToanThatBai", "Pay", new { area = "KhachHang" });
            }

            // Lấy vnp_SecureHash từ query (xử lý null)
            var vnp_SecureHash = vnp_Params["vnp_SecureHash"].ToString() ?? "";

            // Lấy queryStringWithoutHash an toàn hơn
            string queryStringWithoutHash = "";
            if (!string.IsNullOrEmpty(queryString))
            {
                 var queryParams = queryString.TrimStart('?')
                                           .Split('&')
                                           .Where(param => !param.StartsWith("vnp_SecureHash=") && !param.StartsWith("vnp_SecureHashType="));
                queryStringWithoutHash = string.Join("&", queryParams);
            }

            // Xác thực chữ ký (xử lý null)
            bool isValidSignature = _vnpayService.ValidateSignature(
                queryStringWithoutHash,
                vnp_SecureHash,
                vnp_HashSecret
            );

            if (!isValidSignature)
            {
                Console.WriteLine("VNPAY Return: Invalid Signature");
                // Chuyển đến trang lỗi với tham số area
                return RedirectToAction("ThanhToanThatBai", "Pay", new { area = "KhachHang" });
            }

            // Lấy các tham số khác (xử lý null)
            var vnp_ResponseCode = vnp_Params["vnp_ResponseCode"].ToString() ?? "";
            var vnp_TxnRef = vnp_Params["vnp_TxnRef"].ToString() ?? "";
            var vnp_OrderInfo = vnp_Params["vnp_OrderInfo"].ToString() ?? "";

            if (vnp_ResponseCode == "00")
            {
                Console.WriteLine("VNPAY Return: Success (Code 00)");
                
                // Lấy TxnRef từ OrderInfo
                string txnRefFromOrderInfo = vnp_OrderInfo.Replace("Payment_", "");
                if (string.IsNullOrEmpty(txnRefFromOrderInfo))
                {
                    txnRefFromOrderInfo = vnp_TxnRef;
                }
                
                // Đợi 3 giây cho IPN tạo đơn hàng
                await Task.Delay(3000);
                
                // Tìm đơn hàng qua TxnRef trong MaPhanHoi
                var thanhToan = await _context.ThanhToans
                    .Include(tt => tt.IdDonHangNavigation)
                    .Where(tt => tt.PhuongThuc == "VNPAY" 
                              && tt.TrangThai == "ThanhCong"
                              && tt.MaPhanHoi != null
                              && tt.MaPhanHoi.Contains(txnRefFromOrderInfo))
                    .OrderByDescending(tt => tt.NgayTao)
                    .FirstOrDefaultAsync();
                
                var donHang = thanhToan?.IdDonHangNavigation;
                
                if (donHang != null)
                {
                    Console.WriteLine($"[VNPAY Return] Found order: DH{donHang.IdDonHang}");
                    return RedirectToAction("DatHangThanhCong", "DonHang", new { area = "KhachHang", orderCode = $"dh{donHang.IdDonHang:D6}" });
                }
                
                // Fallback: Tìm đơn hàng VNPAY mới nhất
                donHang = await _context.DonHangs
                    .Where(dh => dh.PhuongThucThanhToan == "VNPAY" 
                              && dh.NgayDat >= DateTime.Now.AddMinutes(-2))
                    .OrderByDescending(dh => dh.NgayDat)
                    .FirstOrDefaultAsync();
                
                if (donHang != null)
                {
                    Console.WriteLine($"[VNPAY Return] Found recent order: DH{donHang.IdDonHang}");
                    return RedirectToAction("DatHangThanhCong", "DonHang", new { area = "KhachHang", orderCode = $"dh{donHang.IdDonHang:D6}" });
                }
                
                // Không tìm thấy - về trang đơn hàng
                Console.WriteLine("[VNPAY Return] Order not found");
                return RedirectToAction("Index", "DonHang", new { area = "KhachHang" });
            }
            else
            {
                Console.WriteLine($"VNPAY Return: Failed (Code {vnp_ResponseCode})");
                // Chuyển đến trang lỗi với tham số area
                return RedirectToAction("ThanhToanThatBai", "Pay", new { area = "KhachHang" });
            }
        }

        // Action này là nơi VNPAY gọi ngầm (IPN - Instant Payment Notification)
        // URL: /KhachHang/Vnpay/PaymentIpn
        [AllowAnonymous]
        [HttpGet] // VNPAY IPN thường gọi bằng GET
        public async Task<IActionResult> PaymentIpn()
        {
            Console.WriteLine($"\n\n>>>>>>>>>>>>> VNPAY IPN ACTION HIT at {DateTime.Now} <<<<<<<<<<<<<\n");
            
            // Lấy HashSecret từ config (xử lý null)
            string vnp_HashSecret = _config["Vnpay:HashSecret"] ?? "";

            var vnp_Params = HttpContext.Request.Query;
            var queryString = HttpContext.Request.QueryString.Value ?? ""; // Xử lý null

            Console.WriteLine("=== VNPAY IPN CALLED ===");

            if (string.IsNullOrEmpty(queryString) || vnp_Params.Count == 0)
            {
                 Console.WriteLine("IPN Error: Input data required");
                 return Json(new { RspCode = "99", Message = "Input data required" });
            }

            // Lấy vnp_SecureHash từ query (xử lý null)
            var vnp_SecureHash = vnp_Params["vnp_SecureHash"].ToString() ?? "";

            // Lấy queryStringWithoutHash an toàn hơn
            string queryStringWithoutHash = "";
             if (!string.IsNullOrEmpty(queryString))
            {
                 var queryParams = queryString.TrimStart('?')
                                           .Split('&')
                                           .Where(param => !param.StartsWith("vnp_SecureHash=") && !param.StartsWith("vnp_SecureHashType="));
                queryStringWithoutHash = string.Join("&", queryParams);
            }

            // Xác thực chữ ký (xử lý null)
            bool isValidSignature = _vnpayService.ValidateSignature(
                queryStringWithoutHash,
                vnp_SecureHash,
                vnp_HashSecret
            );

            if (!isValidSignature)
            {
                Console.WriteLine("IPN Error: Invalid Signature");
                // Quan trọng: Phải trả về lỗi 97 để VNPAY biết chữ ký sai
                return Json(new { RspCode = "97", Message = "Invalid Signature" });
            }

            // --- CHỮ KÝ HỢP LỆ, TIẾN HÀNH XỬ LÝ DATABASE ---
            // Lấy các tham số (xử lý null)
            var vnp_ResponseCode = vnp_Params["vnp_ResponseCode"].ToString() ?? "";
            var vnp_TxnRef = vnp_Params["vnp_TxnRef"].ToString() ?? ""; // TxnRef (timestamp ID)
            var vnp_TransactionNo = vnp_Params["vnp_TransactionNo"].ToString() ?? ""; // Mã GD của VNPAY
            var vnp_Amount_Raw = vnp_Params["vnp_Amount"].ToString() ?? "0"; // Lấy dạng string, mặc định là "0"
            var vnp_BankCode = vnp_Params["vnp_BankCode"].ToString() ?? "";
            var vnp_OrderInfo = vnp_Params["vnp_OrderInfo"].ToString() ?? ""; // Lấy OrderInfo

            // Chuyển đổi Amount an toàn
            decimal vnp_Amount = 0;
            if (decimal.TryParse(vnp_Amount_Raw, out decimal rawAmount))
            {
                 vnp_Amount = rawAmount / 100;
            }

            Console.WriteLine($"[VNPAY IPN] TxnRef={vnp_TxnRef}, ResponseCode={vnp_ResponseCode}, Amount={vnp_Amount}, OrderInfo={vnp_OrderInfo}");

            // Lấy TxnRef từ OrderInfo (format: "Payment_<TxnRef>")
            string txnRefFromOrderInfo = vnp_OrderInfo.Replace("Payment_", "");
            if (string.IsNullOrEmpty(txnRefFromOrderInfo))
            {
                txnRefFromOrderInfo = vnp_TxnRef; // Fallback to vnp_TxnRef
            }

            // Lấy metadata từ cache
            string cacheKey = $"VNPAY_ORDER_{txnRefFromOrderInfo}";
            string? metadataJson = _memoryCache.Get<string>(cacheKey);

            if (string.IsNullOrEmpty(metadataJson))
            {
                Console.WriteLine($"[VNPAY IPN] Error: Metadata not found in cache for TxnRef={txnRefFromOrderInfo}");
                return Json(new { RspCode = "02", Message = "Order metadata not found" });
            }

            Console.WriteLine($"[VNPAY IPN] Found metadata in cache: {metadataJson}");

            // Deserialize metadata
            var metadata = System.Text.Json.JsonSerializer.Deserialize<VnpayOrderMetadata>(metadataJson);
            if (metadata == null || metadata.OrderItems == null || !metadata.OrderItems.Any())
            {
                Console.WriteLine("[VNPAY IPN] Error: Invalid metadata structure");
                return Json(new { RspCode = "02", Message = "Invalid order metadata" });
            }

            // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Chỉ xử lý thanh toán thành công
                if (vnp_ResponseCode != "00")
                {
                    Console.WriteLine($"[VNPAY IPN] Payment failed with code {vnp_ResponseCode}. No order created.");
                    // Xóa metadata khỏi cache
                    _memoryCache.Remove(cacheKey);
                    return Json(new { RspCode = "00", Message = "Payment failed - confirmed" });
                }

                // === THANH TOÁN THÀNH CÔNG - TẠO ĐƠN HÀNG ===
                Console.WriteLine($"[VNPAY IPN] Payment SUCCESS. Creating order for UserID={metadata.UserId}");

                // 1. Validate địa chỉ
                var address = await _context.DiaChis
                    .FirstOrDefaultAsync(d => d.IdDiaChi == metadata.AddressId && d.IdTaiKhoan == metadata.UserId);

                if (address == null)
                {
                    Console.WriteLine($"[VNPAY IPN] Error: Invalid address ID={metadata.AddressId}");
                    await transaction.RollbackAsync();
                    _memoryCache.Remove(cacheKey);
                    return Json(new { RspCode = "02", Message = "Invalid address" });
                }

                // 2. Validate tồn kho
                foreach (var item in metadata.OrderItems)
                {
                    var bienThe = await _context.BienTheSanPhams
                        .Include(bt => bt.IdSanPhamNavigation)
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == item.IdBienThe);

                    if (bienThe == null || bienThe.IdSanPhamNavigation?.TrangThai != true)
                    {
                        Console.WriteLine($"[VNPAY IPN] Error: Product ID={item.IdBienThe} not found or inactive");
                        await transaction.RollbackAsync();
                        _memoryCache.Remove(cacheKey);
                        return Json(new { RspCode = "02", Message = "Product not available" });
                    }

                    if (bienThe.SoLuongTonKho < item.SoLuong)
                    {
                        Console.WriteLine($"[VNPAY IPN] Error: Insufficient stock for ID={item.IdBienThe}. Need={item.SoLuong}, Have={bienThe.SoLuongTonKho}");
                        await transaction.RollbackAsync();
                        _memoryCache.Remove(cacheKey);
                        return Json(new { RspCode = "02", Message = "Insufficient stock" });
                    }
                }

                // 3. Tạo đơn hàng
                var donHang = new DonHang
                {
                    IdTaiKhoan = metadata.UserId,
                    IdDiaChi = metadata.AddressId,
                    NgayDat = DateTime.Now,
                    PhuongThucThanhToan = "VNPAY",
                    TongTien = metadata.TotalAmount,
                    TrangThai = "Chờ xác nhận"
                };
                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync(); // Lưu để lấy IdDonHang

                Console.WriteLine($"[VNPAY IPN] Created order ID={donHang.IdDonHang}");

                // 4. Tạo ChiTietDonHang và trừ kho
                foreach (var item in metadata.OrderItems)
                {
                    var chiTiet = new ChiTietDonHang
                    {
                        IdDonHang = donHang.IdDonHang,
                        IdBienThe = item.IdBienThe,
                        SoLuong = item.SoLuong,
                        GiaLucDat = item.DonGia
                    };
                    _context.ChiTietDonHangs.Add(chiTiet);

                    // Trừ kho
                    var bienThe = await _context.BienTheSanPhams
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == item.IdBienThe);
                    if (bienThe != null)
                    {
                        bienThe.SoLuongTonKho -= item.SoLuong;
                        Console.WriteLine($"[VNPAY IPN] Reduced stock for ID={item.IdBienThe}: {bienThe.SoLuongTonKho + item.SoLuong} -> {bienThe.SoLuongTonKho}");
                    }
                }

                // 5. Tạo bản ghi ThanhToan
                var thanhToan = new ThanhToan
                {
                    IdDonHang = donHang.IdDonHang,
                    PhuongThuc = "VNPAY",
                    TrangThai = "ThanhCong",
                    MaGiaoDichNganHang = vnp_TransactionNo,
                    MaNganHang = vnp_BankCode,
                    MaPhanHoi = vnp_ResponseCode,
                    SoTien = metadata.TotalAmount,
                    NgayTao = DateTime.Now,
                    ThoiGianThanhToan = DateTime.Now
                };
                _context.ThanhToans.Add(thanhToan);

                // 6. Tạo Timeline
                var initialTimeline = new TimelineDonHang
                {
                    IdDonHang = donHang.IdDonHang,
                    TrangThaiMoi = "Chờ xác nhận",
                    GhiChu = "Đơn hàng đã được thanh toán thành công qua VNPAY.",
                    NgayCapNhat = DateTime.Now
                };
                _context.TimelineDonHangs.Add(initialTimeline);

                // 7. Xóa sản phẩm khỏi giỏ hàng
                var gioHang = await _context.GioHangs
                    .Include(gh => gh.ChiTietGioHangs)
                    .FirstOrDefaultAsync(gh => gh.IdTaiKhoan == metadata.UserId);

                if (gioHang != null)
                {
                    var bienTheIds = metadata.OrderItems.Select(item => item.IdBienThe).ToList();
                    var itemsToRemove = gioHang.ChiTietGioHangs
                        .Where(ct => ct.IdBienThe.HasValue && bienTheIds.Contains(ct.IdBienThe.Value))
                        .ToList();

                    if (itemsToRemove.Any())
                    {
                        _context.ChiTietGioHangs.RemoveRange(itemsToRemove);
                        gioHang.NgayCapNhat = DateTime.Now;
                        Console.WriteLine($"[VNPAY IPN] Removed {itemsToRemove.Count} items from cart");
                    }
                }

                // 8. Lưu tất cả và commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"[VNPAY IPN] Order created successfully: DH{donHang.IdDonHang}");

                // 9. Xóa metadata khỏi cache
                _memoryCache.Remove(cacheKey);

                // 10. Gửi email (reload order với navigation properties)
                var orderForEmail = await _context.DonHangs
                    .Include(dh => dh.IdTaiKhoanNavigation)
                    .Include(dh => dh.IdDiaChiNavigation)
                    .Include(dh => dh.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                            .ThenInclude(bt => bt.IdSanPhamNavigation)
                                .ThenInclude(sp => sp.AnhSanPhams)
                    .Include(dh => dh.ChiTietDonHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                            .ThenInclude(bt => bt.IdGiaTris)
                                .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .FirstOrDefaultAsync(dh => dh.IdDonHang == donHang.IdDonHang);

                if (orderForEmail != null)
                {
                    try
                    {
                        await _orderEmailService.SendOrderConfirmationEmailAsync(orderForEmail);
                        Console.WriteLine($"[VNPAY IPN] Email sent to {orderForEmail.IdTaiKhoanNavigation?.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[VNPAY IPN] Warning: Failed to send email - {emailEx.Message}");
                    }
                }

                return Json(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (Exception ex)
            {
                // Rollback nếu có lỗi
                try { await transaction.RollbackAsync(); }
                catch (Exception rbEx) { Console.WriteLine($"[VNPAY IPN] Rollback failed: {rbEx.Message}"); }

                Console.WriteLine($"[VNPAY IPN] Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new { RspCode = "99", Message = "Unknown error during processing" });
            }
        }
    }

    // ============= VNPAY METADATA CLASSES =============
    public class VnpayOrderMetadata
    {
        public int UserId { get; set; }
        public int AddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<VnpayOrderItem> OrderItems { get; set; } = new List<VnpayOrderItem>();
    }

    public class VnpayOrderItem
    {
        public int IdBienThe { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}