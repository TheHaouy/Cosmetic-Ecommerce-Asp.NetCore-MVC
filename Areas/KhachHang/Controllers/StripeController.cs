using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Final_VS1.Data; // Namespace cho DbContext
using Final_VS1.Areas.KhachHang.Models; // Namespace cho ThanhToan
using Final_VS1.Areas.KhachHang.Services; // Service layer
using Stripe; // Thư viện Stripe.net
using Microsoft.Extensions.Configuration; // Dùng IConfiguration
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO; // Dùng cho StreamReader

// Model request đơn giản từ JavaScript (chúng ta sẽ tạo ở Bước 5)
// Tạm thời, chúng ta sẽ tính lại giỏ hàng từ DB
public class StripePaymentIntentRequest
{
    public int AddressId { get; set; }
    public List<OrderItemRequest>? OrderItems { get; set; } // Thêm để hỗ trợ "Mua ngay"
}

public class OrderItemRequest
{
    public int IdBienThe { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize] // Yêu cầu đăng nhập
    public class StripeController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly IConfiguration _config;
        private readonly string? _stripeSecretKey;
        private readonly string? _webhookSecret; // Khóa bí mật cho Webhook
        private readonly IOrderEmailService _orderEmailService;
        public StripeController(LittleFishBeautyContext context, IConfiguration config, IOrderEmailService orderEmailService)
        {
            _context = context;
            _config = config;
            _orderEmailService = orderEmailService;
            // Lấy Secret Key từ appsettings.json
            _stripeSecretKey = _config["Stripe:SecretKey"];
            // Lấy Webhook Secret từ appsettings.json (CHÚNG TA SẼ LẤY KHÓA NÀY SAU)
            _webhookSecret = _config["Stripe:WebhookSecret"]; 
            
            // Cấu hình API Key cho toàn bộ thư viện Stripe
            if (!string.IsNullOrEmpty(_stripeSecretKey))
            {
                StripeConfiguration.ApiKey = _stripeSecretKey;
            }
        }

        // API: /KhachHang/Stripe/GetSavedPaymentMethods
        // Lấy danh sách payment methods đã lưu của customer
        [HttpGet]
        public async Task<IActionResult> GetSavedPaymentMethods()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            try
            {
                var user = await _context.TaiKhoans.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    return Json(new { success = true, paymentMethods = new List<object>() });
                }

                var pmService = new PaymentMethodService();
                var paymentMethods = await pmService.ListAsync(new PaymentMethodListOptions
                {
                    Customer = user.StripeCustomerId,
                    Type = "card"
                });

                var methods = paymentMethods.Data.Select(pm => new
                {
                    id = pm.Id,
                    brand = pm.Card.Brand,
                    last4 = pm.Card.Last4,
                    expMonth = pm.Card.ExpMonth,
                    expYear = pm.Card.ExpYear
                }).ToList();

                return Json(new { success = true, paymentMethods = methods });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stripe] Error getting saved payment methods: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: /KhachHang/Stripe/CreatePaymentIntent
        // Được gọi bởi JavaScript khi người dùng chọn Stripe
        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] StripePaymentIntentRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            // Kiểm tra cấu hình Stripe
            if (string.IsNullOrEmpty(_stripeSecretKey))
            {
                return Json(new { success = false, message = "Cấu hình Stripe không hợp lệ" });
            }

            // === 1. TÍNH TOÁN LẠI TỔNG TIỀN (LOGIC GIỐNG HỆT PAYCONTROLLER) ===
            
            decimal totalAmountVND = 0;
            List<OrderItemRequest> validatedItems = new List<OrderItemRequest>();
            
            // Kiểm tra xem có phải là "Mua ngay" không
            bool isBuyNow = request.OrderItems != null && request.OrderItems.Any();
            
            if (isBuyNow)
            {
                // === LOGIC CHO MUA NGAY ===
                Console.WriteLine($"[Stripe] Buy Now mode - {request.OrderItems!.Count} items");
                
                foreach (var item in request.OrderItems!)
                {
                    if (item.IdBienThe <= 0 || item.SoLuong <= 0)
                    {
                        return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ" });
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
                        return Json(new { success = false, message = $"Sản phẩm {bienThe.IdSanPhamNavigation?.TenSanPham ?? "ID " + item.IdBienThe} không đủ số lượng trong kho" });
                    }

                    var currentPrice = bienThe.GiaBan ?? 0m;
                    if (currentPrice <= 0)
                    {
                        return Json(new { success = false, message = $"Sản phẩm có giá không hợp lệ" });
                    }
                    
                    totalAmountVND += currentPrice * item.SoLuong;
                    validatedItems.Add(new OrderItemRequest
                    {
                        IdBienThe = item.IdBienThe,
                        SoLuong = item.SoLuong,
                        DonGia = currentPrice
                    });
                }
            }
            else
            {
                // === LOGIC CHO GIỎ HÀNG THÔNG THƯỜNG ===
                var gioHang = await _context.GioHangs
                    .Include(gh => gh.ChiTietGioHangs)
                        .ThenInclude(ct => ct.IdBienTheNavigation)
                            .ThenInclude(bt => bt!.IdSanPhamNavigation)
                    .FirstOrDefaultAsync(gh => gh.IdTaiKhoan == userId);

                if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                foreach (var item in gioHang.ChiTietGioHangs)
                {
                    var bienThe = item.IdBienTheNavigation;
                    if (bienThe == null || bienThe.IdSanPhamNavigation?.TrangThai != true)
                    {
                        return Json(new { success = false, message = "Một sản phẩm trong giỏ không hợp lệ" });
                    }
                    var currentPrice = bienThe.GiaBan ?? 0;
                    totalAmountVND += currentPrice * (item.SoLuong ?? 0);
                    
                    validatedItems.Add(new OrderItemRequest
                    {
                        IdBienThe = item.IdBienThe ?? 0,
                        SoLuong = item.SoLuong ?? 0,
                        DonGia = currentPrice
                    });
                }
            }
            
            // TODO: Cộng phí vận chuyển (nếu có)
            // totalAmountVND += 30000; 

            if (totalAmountVND <= 0)
            {
                 return Json(new { success = false, message = "Tổng tiền không hợp lệ" });
            }
            
            // === 2. QUY ĐỔI SANG USD (Stripe xử lý tốt nhất với USD) ===
            // Tạm thời dùng tỉ giá cố định, thực tế bạn nên gọi API tỉ giá
            // VÀ Stripe tính tiền bằng đơn vị nhỏ nhất (cents), nên phải * 100
            decimal exchangeRate = 25000; // Ví dụ: 1 USD = 25.000 VND
            long totalAmountUSDInCents = (long)((totalAmountVND / exchangeRate) * 100); 
            
            // Stripe yêu cầu số tiền tối thiểu (thường là 0.50 USD)
            if (totalAmountUSDInCents < 50) 
            {
                totalAmountUSDInCents = 50; // Set mức tối thiểu 50 cents
            }

            // === 3. KHÔNG TẠO ĐƠN HÀNG NGAY - Chỉ chuẩn bị metadata ===
            // Đơn hàng sẽ được tạo trong Webhook khi thanh toán thành công
            // Lưu thông tin vào metadata để webhook sử dụng
            Console.WriteLine("[Stripe] Preparing order data for webhook - NOT creating order yet");
            
            // Serialize order items để lưu vào metadata
            var orderItemsJson = System.Text.Json.JsonSerializer.Serialize(validatedItems.Select(i => new {
                idBienThe = i.IdBienThe,
                soLuong = i.SoLuong,
                donGia = i.DonGia
            }));

            // --- KIỂM TRA VÀ LẤY CUSTOMER ID ---
            var user = await _context.TaiKhoans.FindAsync(userId);
            string stripeCustomerId = "";

            if (user != null)
            {
                stripeCustomerId = await GetOrCreateStripeCustomer(user.IdTaiKhoan, user.Email ?? "", user.HoTen ?? "");
                Console.WriteLine($"[Stripe] Using Customer ID: {stripeCustomerId} for user {userId}");
                
                // Kiểm tra xem customer có saved payment methods không
                try
                {
                    var pmService = new PaymentMethodService();
                    var paymentMethods = await pmService.ListAsync(new PaymentMethodListOptions
                    {
                        Customer = stripeCustomerId,
                        Type = "card"
                    });
                    Console.WriteLine($"[Stripe] Customer has {paymentMethods.Data.Count} saved payment method(s)");
                    foreach (var pm in paymentMethods.Data)
                    {
                        Console.WriteLine($"  - Card ending in {pm.Card.Last4}, brand: {pm.Card.Brand}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stripe] Could not list payment methods: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[Stripe] WARNING: User {userId} not found in database!");
            }

            // === 4. TẠO PAYMENT INTENT TRÊN STRIPE ===
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = totalAmountUSDInCents,
                    Currency = "usd",
                    
                    // QUAN TRỌNG: Gắn Customer ID để Stripe hiển thị saved cards
                    Customer = !string.IsNullOrEmpty(stripeCustomerId) ? stripeCustomerId : null,
                    
                    // QUAN TRỌNG: off_session để lưu thẻ cho tương lai
                    SetupFutureUsage = "off_session",
                    
                    // Chỉ dùng card payment method
                    PaymentMethodTypes = new List<string> { "card" },

                    // Cấu hình cho Card
                    PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
                    {
                        Card = new PaymentIntentPaymentMethodOptionsCardOptions
                        {
                            RequestThreeDSecure = "automatic",
                            SetupFutureUsage = "off_session"
                        }
                    },

                    // Metadata - LƯU THÔNG TIN ĐỂ WEBHOOK TẠO ĐƠN HÀNG
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserID", userId.Value.ToString() },
                        { "AddressID", request.AddressId.ToString() },
                        { "TotalAmountVND", totalAmountVND.ToString() },
                        { "OrderItems", orderItemsJson } // Lưu chi tiết đơn hàng
                    }
                };
                
                Console.WriteLine($"[Stripe] Creating PaymentIntent:");
                Console.WriteLine($"  - Amount: ${totalAmountUSDInCents / 100.0:F2} USD");
                Console.WriteLine($"  - Customer: {stripeCustomerId}");
                Console.WriteLine($"  - SetupFutureUsage: off_session");
                Console.WriteLine($"  - Will create order in webhook after payment success");
                
                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                Console.WriteLine($"[Stripe] PaymentIntent created: {paymentIntent.Id}");

                // Trả về client_secret - KHÔNG CÓ orderId vì chưa tạo đơn
                return Json(new { 
                    success = true, 
                    clientSecret = paymentIntent.ClientSecret,
                    paymentIntentId = paymentIntent.Id, // Trả về PI ID để track
                    customerId = stripeCustomerId
                });
            }
            catch (StripeException e)
            {
                Console.WriteLine($"[Stripe] Error creating PaymentIntent: {e.Message}");
                return Json(new { success = false, message = e.Message });
            }
        }

        // API: /KhachHang/Stripe/Webhook
        // Được gọi bởi Server của Stripe (giống IPN của VNPAY)
        [HttpPost]
        [AllowAnonymous] // Cho phép Stripe gọi mà không cần đăng nhập
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            // Lấy khóa bí mật webhook từ config
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                 Console.WriteLine("--- STRIPE WEBHOOK ERROR: WebhookSecret is not configured in appsettings.json ---");
                 return BadRequest("Webhook secret not configured.");
            }

            try
            {
                // Xác thực chữ ký webhook
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret // <-- Khóa bí mật Webhook
                );

                Console.WriteLine($"--- STRIPE WEBHOOK HIT: {stripeEvent.Type} ---");

                // Chỉ xử lý sự kiện thanh toán thành công
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent == null) {
                         return BadRequest("Invalid PaymentIntent object");
                    }

                    Console.WriteLine($"[Stripe Webhook] Payment succeeded: {paymentIntent.Id}");

                    // Lấy thông tin từ metadata
                    if (paymentIntent.Metadata != null &&
                        paymentIntent.Metadata.TryGetValue("UserID", out string? userIdStr) &&
                        paymentIntent.Metadata.TryGetValue("AddressID", out string? addressIdStr) &&
                        paymentIntent.Metadata.TryGetValue("TotalAmountVND", out string? totalAmountStr) &&
                        paymentIntent.Metadata.TryGetValue("OrderItems", out string? orderItemsJson) &&
                        int.TryParse(userIdStr, out int userId) &&
                        int.TryParse(addressIdStr, out int addressId) &&
                        decimal.TryParse(totalAmountStr, out decimal totalAmount))
                    {
                        Console.WriteLine($"[Stripe Webhook] Creating order for user {userId}");
                        
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        
                        try
                        {
                            // === TẠO ĐƠN HÀNG MỚI ===
                            var donHang = new DonHang
                            {
                                IdTaiKhoan = userId,
                                IdDiaChi = addressId,
                                NgayDat = DateTime.Now,
                                PhuongThucThanhToan = "STRIPE",
                                TongTien = totalAmount,
                                TrangThai = "Chờ xác nhận" // Đơn hàng thành công ngay
                            };

                            // Thêm chi tiết đơn hàng từ metadata
                            var orderItems = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(orderItemsJson);
                            if (orderItems != null)
                            {
                                foreach (var item in orderItems)
                                {
                                    var itemElement = (System.Text.Json.JsonElement)item;
                                    donHang.ChiTietDonHangs.Add(new ChiTietDonHang
                                    {
                                        IdBienThe = itemElement.GetProperty("idBienThe").GetInt32(),
                                        SoLuong = itemElement.GetProperty("soLuong").GetInt32(),
                                        GiaLucDat = itemElement.GetProperty("donGia").GetDecimal()
                                    });
                                }
                            }

                            _context.DonHangs.Add(donHang);
                            await _context.SaveChangesAsync();

                            Console.WriteLine($"[Stripe Webhook] Order created: DH{donHang.IdDonHang}");

                            // === TẠO BẢN GHI THANH TOÁN ===
                            var thanhToan = new ThanhToan
                            {
                                IdDonHang = donHang.IdDonHang,
                                PhuongThuc = "STRIPE",
                                TrangThai = "ThanhCong",
                                SoTien = totalAmount,
                                MaGiaoDichNganHang = paymentIntent.Id,
                                MaPhanHoi = paymentIntent.Status,
                                ThoiGianThanhToan = DateTime.Now,
                                NgayTao = DateTime.Now
                            };
                            _context.ThanhToans.Add(thanhToan);
                            await _context.SaveChangesAsync();

                            // === TRỪ KHO ===
                            foreach (var item in donHang.ChiTietDonHangs)
                            {
                                var bienThe = await _context.BienTheSanPhams
                                    .FirstOrDefaultAsync(bt => bt.IdBienThe == item.IdBienThe);
                                if (bienThe != null && bienThe.SoLuongTonKho >= item.SoLuong)
                                {
                                    bienThe.SoLuongTonKho -= item.SoLuong;
                                    Console.WriteLine($"[Stripe Webhook] Reduced stock: BienThe {item.IdBienThe} - {item.SoLuong}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Stripe Webhook] WARNING: Insufficient stock for BienThe {item.IdBienThe}");
                                    if(bienThe != null) bienThe.SoLuongTonKho = 0;
                                }
                            }

                            // === XÓA GIỎ HÀNG (nếu mua từ giỏ hàng) ===
                            var gioHang = await _context.GioHangs
                                .Include(gh => gh.ChiTietGioHangs)
                                .FirstOrDefaultAsync(gh => gh.IdTaiKhoan == donHang.IdTaiKhoan);

                            if (gioHang != null && gioHang.ChiTietGioHangs.Any())
                            {
                                var bienTheIds = donHang.ChiTietDonHangs.Select(ct => ct.IdBienThe).ToList();
                                var itemsToRemove = gioHang.ChiTietGioHangs
                                    .Where(ct => ct.IdBienThe.HasValue && bienTheIds.Contains(ct.IdBienThe.Value))
                                    .ToList();
                                
                                if (itemsToRemove.Any())
                                {
                                    _context.ChiTietGioHangs.RemoveRange(itemsToRemove);
                                    gioHang.NgayCapNhat = DateTime.Now;
                                    Console.WriteLine($"[Stripe Webhook] Removed {itemsToRemove.Count} items from cart");
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            // Gửi email xác nhận đơn hàng
                            try
                            {
                                // Reload đơn hàng với navigation properties để gửi email (bao gồm ảnh sản phẩm)
                                var donHangForEmail = await _context.DonHangs
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
                                
                                if (donHangForEmail != null)
                                {
                                    await _orderEmailService.SendOrderConfirmationEmailAsync(donHangForEmail);
                                    Console.WriteLine($"[Stripe Webhook] Email sent to {donHangForEmail.IdTaiKhoanNavigation?.Email}");
                                }
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"[Stripe Webhook] Email error: {emailEx.Message}");
                            }
                            
                            Console.WriteLine($"[Stripe Webhook] ✅ Order DH{donHang.IdDonHang} created successfully");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($"[Stripe Webhook] ❌ Error creating order: {ex.Message}");
                        }
                    }
                    else
                    {
                         Console.WriteLine($"[Stripe Webhook] ❌ Missing required metadata in PaymentIntent {paymentIntent.Id}");
                    }
                }
                else
                {
                    // Xử lý các sự kiện khác nếu cần (ví dụ: payment_intent.payment_failed)
                    Console.WriteLine($"--- STRIPE WEBHOOK: Sự kiện chưa xử lý: {stripeEvent.Type} ---");
                }

                // Trả về 200 OK để báo cho Stripe biết là đã nhận được
                return Ok(); 
            }
            catch (StripeException e)
            {
                // Lỗi xác thực chữ ký
                Console.WriteLine($"--- STRIPE WEBHOOK ERROR (Signature): {e.Message} ---");
                return BadRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- STRIPE WEBHOOK ERROR (General): {ex.Message}");
                return StatusCode(500); // Lỗi server
            }
        }
        
        // Hàm hỗ trợ: Lấy ID khách hàng Stripe từ DB, nếu chưa có thì tạo mới trên Stripe
        private async Task<string> GetOrCreateStripeCustomer(int userId, string email, string name)
        {
            var user = await _context.TaiKhoans.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"[Stripe] User {userId} not found in database");
                return "";
            }

            // Nếu đã có ID trong DB, trả về luôn
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                Console.WriteLine($"[Stripe] Found existing Stripe Customer ID for user {userId}: {user.StripeCustomerId}");
                
                // Xác minh customer còn tồn tại trên Stripe
                try
                {
                    var customerService = new CustomerService();
                    var existingCustomer = await customerService.GetAsync(user.StripeCustomerId);
                    Console.WriteLine($"[Stripe] Verified customer exists on Stripe: {existingCustomer.Id}");
                    return user.StripeCustomerId;
                }
                catch (StripeException ex)
                {
                    Console.WriteLine($"[Stripe] Customer {user.StripeCustomerId} not found on Stripe: {ex.Message}. Creating new one...");
                    // Nếu customer không tồn tại trên Stripe, tạo mới
                }
            }

            // Nếu chưa có, tạo mới trên Stripe
            Console.WriteLine($"[Stripe] Creating new Stripe Customer for user {userId} with email {email}");
            var customerOptions = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Description = $"Khách hàng ID: {userId}",
                // Metadata để dễ tra cứu
                Metadata = new Dictionary<string, string>
                {
                    { "UserID", userId.ToString() },
                    { "SystemEmail", email }
                }
            };
            
            var customerService2 = new CustomerService();
            var customer = await customerService2.CreateAsync(customerOptions);
            
            Console.WriteLine($"[Stripe] Created new customer: {customer.Id}");

            // Lưu ID mới vào Database
            user.StripeCustomerId = customer.Id;
            // Chỉ update cột này để an toàn
            _context.Entry(user).Property(x => x.StripeCustomerId).IsModified = true; 
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[Stripe] Saved customer ID to database for user {userId}");

            return customer.Id;
        }

        // Hàm helper lấy User ID (Giống PayController)
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
    }
}