using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Final_VS1.Data; // Đảm bảo bạn đã có 'using' này
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Final_VS1.Areas.KhachHang.Services; // <-- THÊM DÒNG NÀY (hoặc đã có)
using System.Collections.Generic; // <-- Thêm using này nếu chưa có
using System.Linq; // <-- Thêm using này nếu chưa có
using System; // <-- Thêm using này nếu chưa có
using System.Threading.Tasks; // <-- Thêm using này nếu chưa có
using Microsoft.Extensions.Caching.Memory; // <-- THÊM CHO MEMORY CACHE

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class PayController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly VnpayService _vnpayService; // <-- THÊM DÒNG NÀY
        private readonly IMemoryCache _memoryCache; // <-- THÊM CHO VNPAY METADATA

        // SỬA CONSTRUCTOR ĐỂ TIÊM VNPAYSERVICE
        public PayController(LittleFishBeautyContext context, VnpayService vnpayService, IMemoryCache memoryCache) // <-- SỬA CONSTRUCTOR NÀY
        {
            _context = context;
            _vnpayService = vnpayService; // <-- THÊM DÒNG NÀY
            _memoryCache = memoryCache; // <-- THÊM DÒNG NÀY
        }

        public IActionResult Index()
        {
            Console.WriteLine("=== PayController Index called ===");

            // ... (Logic Index của bạn giữ nguyên) ...
            if (TempData["BuyNowItem"] != null)
            {
                try
                {
                    var buyNowItemJson = TempData["BuyNowItem"]?.ToString();
                    Console.WriteLine($"BuyNowItem found in TempData: {buyNowItemJson}");

                    if (!string.IsNullOrEmpty(buyNowItemJson))
                    {
                        ViewBag.BuyNowItem = buyNowItemJson;
                        ViewBag.IsBuyNow = true;
                        Console.WriteLine("Set ViewBag.IsBuyNow = true");
                    }
                    else
                    {
                        ViewBag.IsBuyNow = false;
                        Console.WriteLine("BuyNowItem is empty, set ViewBag.IsBuyNow = false");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing BuyNowItem: {ex.Message}");
                    ViewBag.IsBuyNow = false;
                }
            }
            else
            {
                Console.WriteLine("No BuyNowItem in TempData, normal checkout mode");
                ViewBag.IsBuyNow = false;
            }

            Console.WriteLine($"ViewBag.IsBuyNow final value: {ViewBag.IsBuyNow}");
            Console.WriteLine($"ViewBag.BuyNowItem final value: {ViewBag.BuyNowItem}");

            return View();
        }

        // Action để hiển thị trang thanh toán thất bại
        // GET: /KhachHang/Pay/ThanhToanThatBai
        // *** THÊM ACTION NÀY ***
        [AllowAnonymous] // Cho phép truy cập ngay cả khi chưa đăng nhập (quan trọng sau khi bị redirect từ VNPAY)
        public IActionResult ThanhToanThatBai()
        {
            // Có thể thêm TempData nếu muốn hiển thị thông báo lỗi cụ thể hơn
            // TempData["PaymentErrorMessage"] = "Giao dịch thanh toán không thành công.";
            return View(); // Trả về view ThanhToanThatBai.cshtml
        }
        // **********************

        // ... (Hàm GetAddressesForPayment và AddAddressFromPayment giữ nguyên) ...
        [HttpGet]
        public async Task<IActionResult> GetAddressesForPayment()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var addresses = await _context.DiaChis
                .Where(d => d.IdTaiKhoan == userId)
                .OrderByDescending(d => d.MacDinh)
                .ThenBy(d => d.IdDiaChi)
                .Select(d => new
                {
                    id = d.IdDiaChi,
                    hoTen = d.HoTenNguoiNhan,
                    soDienThoai = d.SoDienThoai,
                    diaChiChiTiet = d.DiaChiChiTiet,
                    macDinh = d.MacDinh ?? false
                })
                .ToListAsync();

            return Json(new { success = true, addresses });
        }

        [HttpPost]
        public async Task<IActionResult> AddAddressFromPayment([FromBody] AddAddressFromPaymentRequest request)
        {
             // ... (TOÀN BỘ CODE HÀM NÀY CỦA BẠN GIỮ NGUYÊN) ...
            Console.WriteLine("=== AddAddressFromPayment được gọi ===");
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
            }
            // ... (Phần còn lại của hàm giữ nguyên) ...
            try
            {
                var userExists = await _context.TaiKhoans.AnyAsync(tk => tk.IdTaiKhoan == userId);
                if (!userExists)
                {
                    return Json(new { success = false, message = "Tài khoản không hợp lệ." });
                }
                var existingAddressCount = await _context.DiaChis.CountAsync(d => d.IdTaiKhoan == userId);
                var isDefault = existingAddressCount == 0;
                // ... (Logic kiểm tra và set default) ...
                if (isDefault && existingAddressCount > 0) // Sửa lại: chỉ set default nếu là cái đầu tiên
                {
                    // Logic bỏ default cũ
                }
                isDefault = existingAddressCount == 0; // Gán lại isDefault

                var newAddress = new DiaChi
                {
                    IdTaiKhoan = userId.Value, // Giả định userId không null ở đây
                    HoTenNguoiNhan = request.HoTen?.Trim(),
                    SoDienThoai = request.SoDienThoai?.Trim(),
                    DiaChiChiTiet = $"{request.DiaChiChiTiet?.Trim()}, {request.PhuongXa?.Trim()}, {request.TinhThanhPho?.Trim()}",
                    MacDinh = isDefault
                };
                _context.DiaChis.Add(newAddress);
                await _context.SaveChangesAsync();
                return Json(new {
                    success = true,
                    message = "Thêm địa chỉ thành công!",
                    address = new { /* ... dữ liệu address mới ... */ }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thêm địa chỉ: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ. Vui lòng thử lại." });
            }
        }


        // ========= HÀM PROCESSORDER ĐÃ ĐƯỢC CẬP NHẬT VỚI LOGGING =========
        [HttpPost]
        public async Task<IActionResult> ProcessOrder([FromBody] ProcessOrderRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // === THÊM LOG CHI TIẾT VÀO ĐÂY ===
            Console.WriteLine("===== PROCESS ORDER CALLED =====");
            Console.WriteLine($"Raw PaymentMethod received from request: '{request.PaymentMethod}'"); // Log giá trị gốc

            string paymentMethod = request.PaymentMethod ?? "COD"; // Xử lý null như cũ
            Console.WriteLine($"Processed PaymentMethod variable: '{paymentMethod}'"); // Log giá trị đã xử lý
            Console.WriteLine($"Is VNPAY selected (check): {paymentMethod == "VNPAY"}"); // Kiểm tra điều kiện if
            // ===================================

            Console.WriteLine($"ProcessOrder - AddressId: {request.AddressId}");
            Console.WriteLine($"ProcessOrder - OrderItems count: {request.OrderItems?.Count ?? 0}"); // Xử lý null cho OrderItems

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Bước 1: Validate địa chỉ (Giữ nguyên code của bạn)
                var address = await _context.DiaChis
                    .FirstOrDefaultAsync(d => d.IdDiaChi == request.AddressId && d.IdTaiKhoan == userId);

                if (address == null)
                {
                    return Json(new { success = false, message = "Địa chỉ không hợp lệ" });
                }

                 // Validate order items (thêm kiểm tra null)
                if (request.OrderItems == null || !request.OrderItems.Any())
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào để đặt hàng" });
                }


                // Bước 2: Validate tồn kho và tính tổng tiền (Giữ nguyên code của bạn)
                decimal totalAmount = 0;
                var validatedItems = new List<OrderItemRequest>();

                foreach (var item in request.OrderItems)
                {
                    // ... (Code kiểm tra biến thể, tồn kho... của bạn giữ nguyên) ...
                    if (item.IdBienThe <= 0 || item.SoLuong <= 0) // Bỏ kiểm tra DonGia <= 0 vì sẽ lấy từ DB
                    {
                       return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ (ID hoặc Số lượng)" });
                    }

                    var bienThe = await _context.BienTheSanPhams
                        .Include(bt => bt.IdSanPhamNavigation)
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == item.IdBienThe);

                    if (bienThe == null || bienThe.IdSanPhamNavigation?.TrangThai != true)
                    {
                        return Json(new { success = false, message = $"Sản phẩm ID {item.IdBienThe} không tồn tại hoặc đã ngừng bán" });
                    }
                    if (bienThe.SoLuongTonKho < item.SoLuong)
                    {
                        return Json(new { success = false, message = $"Sản phẩm {bienThe.IdSanPhamNavigation?.TenSanPham ?? "ID "+item.IdBienThe} không đủ số lượng trong kho (Còn {bienThe.SoLuongTonKho})" });
                    }

                    var currentPrice = bienThe.GiaBan ?? 0m;
                    if (currentPrice <= 0)
                    {
                         return Json(new { success = false, message = $"Sản phẩm {bienThe.IdSanPhamNavigation?.TenSanPham ?? "ID "+item.IdBienThe} có giá không hợp lệ." });
                    }
                    
                    // Tính giá khuyến mãi nếu có
                    var khuyenMai = await Helpers.PromotionHelper.GetBestPromotionForProduct(_context, bienThe.IdSanPham ?? 0, bienThe.IdSanPhamNavigation?.IdDanhMuc);
                    if (khuyenMai != null)
                    {
                        currentPrice = Helpers.PromotionHelper.CalculatePromotionPrice(currentPrice, khuyenMai);
                    }
                    
                    totalAmount += currentPrice * item.SoLuong;
                    validatedItems.Add(new OrderItemRequest
                    {
                        IdBienThe = item.IdBienThe,
                        SoLuong = item.SoLuong,
                        DonGia = currentPrice // Lưu giá sau khuyến mãi
                    });
                }

                // Bước 3: Tạo đơn hàng
                var donHang = new DonHang
                {
                    IdTaiKhoan = userId.Value, // Đảm bảo userId không null
                    IdDiaChi = request.AddressId,
                    NgayDat = DateTime.Now, // Sử dụng DateTime, không phải DateTime?
                    PhuongThucThanhToan = paymentMethod,
                    TongTien = totalAmount
                    // TrangThai sẽ được set bên dưới
                };

                // Bước 4: PHÂN NHÁNH LOGIC THANH TOÁN
                if (paymentMethod == "VNPAY")
                {
                    Console.WriteLine(">>> Executing VNPAY logic block (NEW FLOW - NO ORDER CREATION)...");

                    // === VNPAY MỚI: KHÔNG TẠO ĐƠN HÀNG, CHỈ LƯU METADATA ===
                    
                    // Tạo unique transaction reference (TxnRef)
                    string txnRef = DateTime.Now.Ticks.ToString();
                    
                    // Serialize order data to JSON (để IPN tạo đơn hàng sau)
                    var orderMetadata = new {
                        UserId = userId.Value,
                        AddressId = request.AddressId,
                        TotalAmount = totalAmount,
                        OrderItems = validatedItems.Select(item => new {
                            item.IdBienThe,
                            item.SoLuong,
                            item.DonGia
                        }).ToList()
                    };
                    string metadataJson = System.Text.Json.JsonSerializer.Serialize(orderMetadata);
                    
                    // Lưu metadata vào MemoryCache với key là TxnRef (expire sau 30 phút)
                    var cacheEntryOptions = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    };
                    _memoryCache.Set($"VNPAY_ORDER_{txnRef}", metadataJson, cacheEntryOptions);
                    
                    Console.WriteLine($"[VNPAY] Saved order metadata to cache with TxnRef: {txnRef}");

                    // Tạo URL VNPAY
                    var vnpayRequest = new VnpayRequestModel
                    {
                        OrderId = int.Parse(txnRef.Substring(txnRef.Length - 9)), // Lấy 9 số cuối
                        Amount = totalAmount,
                        CreatedDate = DateTime.Now,
                        OrderInfo = $"Payment_{txnRef}" // Lưu TxnRef vào OrderInfo
                    };
                    var paymentUrl = _vnpayService.CreatePaymentUrl(HttpContext, vnpayRequest);

                    // Commit transaction (không có gì để commit, nhưng để clean up)
                    await transaction.CommitAsync();

                    Console.WriteLine($"[VNPAY] Payment URL created: {paymentUrl}");

                    // Trả về URL cho frontend
                    return Json(new {
                        success = true,
                        paymentUrl = paymentUrl
                    });
                }
                else // Logic cho COD
                {
                    Console.WriteLine(">>> Executing COD logic block...");

                    // 1. Thiết lập trạng thái đơn hàng
                    donHang.TrangThai = "Chờ xác nhận";
                    _context.DonHangs.Add(donHang);
                    await _context.SaveChangesAsync(); // Lưu để lấy ID_DonHang

                    // 2. TẠO BẢN GHI THANH TOÁN (Quan trọng)
                    // Với COD, ta tạo bản ghi để theo dõi, trạng thái là "ChoThanhToan" (chưa trả tiền)
                    var thanhToan = new ThanhToan
                    {
                        IdDonHang = donHang.IdDonHang,
                        PhuongThuc = "COD",
                        TrangThai = "ChoThanhToan", 
                        SoTien = totalAmount,
                        NgayTao = DateTime.Now
                    };
                    _context.ThanhToans.Add(thanhToan);

                    // 3. Tạo Timeline đầu tiên cho đơn hàng
                    var initialTimeline = new TimelineDonHang
                    {
                        IdDonHang = donHang.IdDonHang,
                        TrangThaiMoi = "Chờ xác nhận",
                        GhiChu = "Đơn hàng mới (Thanh toán khi nhận hàng)",
                        NgayCapNhat = DateTime.Now
                    };
                    _context.TimelineDonHangs.Add(initialTimeline);

                    // 4. Thêm chi tiết đơn hàng và trừ kho
                    foreach (var item in validatedItems)
                    {
                        var chiTiet = new ChiTietDonHang
                        {
                            IdDonHang = donHang.IdDonHang,
                            IdBienThe = item.IdBienThe,
                            SoLuong = item.SoLuong,
                            GiaLucDat = item.DonGia
                        };
                        _context.ChiTietDonHangs.Add(chiTiet);

                        // Cập nhật tồn kho
                        var bienThe = await _context.BienTheSanPhams
                            .FirstOrDefaultAsync(bt => bt.IdBienThe == item.IdBienThe);
                        
                        if (bienThe != null)
                        {
                            if (bienThe.SoLuongTonKho >= item.SoLuong) // Kiểm tra lại lần cuối
                            {
                                bienThe.SoLuongTonKho -= item.SoLuong;
                            }
                            else
                            {
                                // Rollback nếu hết hàng đột ngột
                                await transaction.RollbackAsync();
                                return Json(new { success = false, message = $"Sản phẩm {bienThe.IdSanPhamNavigation?.TenSanPham ?? "ID "+item.IdBienThe} vừa hết hàng. Vui lòng thử lại." });
                            }
                        }
                    }

                    // 5. Xóa sản phẩm khỏi giỏ hàng (Nếu không phải là mua ngay)
                    if (validatedItems.Any() && !(ViewBag.IsBuyNow ?? false))
                    {
                        var gioHang = await _context.GioHangs
                            .Include(gh => gh.ChiTietGioHangs)
                            .FirstOrDefaultAsync(gh => gh.IdTaiKhoan == userId);

                        if (gioHang != null)
                        {
                            var bienTheIds = validatedItems.Select(oi => oi.IdBienThe).ToList();
                            var itemsToRemove = gioHang.ChiTietGioHangs
                                .Where(ct => ct.IdBienThe.HasValue && bienTheIds.Contains(ct.IdBienThe.Value))
                                .ToList();
                            
                            if (itemsToRemove.Any())
                            {
                                _context.ChiTietGioHangs.RemoveRange(itemsToRemove);
                                gioHang.NgayCapNhat = DateTime.Now;
                                Console.WriteLine($"Xóa {itemsToRemove.Count} sản phẩm khỏi giỏ hàng COD");
                            }
                        }
                    }

                    // 6. Lưu tất cả thay đổi và Commit
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.WriteLine($"Đặt hàng COD thành công - OrderId: {donHang.IdDonHang}");

                    // 7. Trả về kết quả thành công
                    return Json(new
                    {
                        success = true,
                        message = "Đặt hàng thành công",
                        orderId = donHang.IdDonHang,
                        orderCode = $"DH{donHang.IdDonHang:D6}"
                    });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Lỗi khi đặt hàng: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại." });
            }
        }

        // ... (Hàm GetCurrentUserId và Request Models giữ nguyên) ...
        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }

        public class AddAddressFromPaymentRequest
        {
            public string? HoTen { get; set; }
            public string? SoDienThoai { get; set; }
            public string? DiaChiChiTiet { get; set; }
            public string? TinhThanhPho { get; set; }
            public string? PhuongXa { get; set; }
        }

        public class ProcessOrderRequest
        {
            public int AddressId { get; set; }
            public string? PaymentMethod { get; set; } // Sẽ là 'COD' hoặc 'VNPAY'
            public decimal TotalAmount { get; set; } // Tổng tiền từ client (chỉ để tham khảo, server sẽ tính lại)
            public string? Note { get; set; }
            public List<OrderItemRequest>? OrderItems { get; set; } // Cho phép null
        }

        public class OrderItemRequest
        {
            public int IdBienThe { get; set; }
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; } // Giá từ client (chỉ để tham khảo, server sẽ lấy từ DB)
        }
    }
}