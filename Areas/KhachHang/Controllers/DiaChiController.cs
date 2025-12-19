using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using System.Security.Claims;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class DiaChiController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DiaChiController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        private int? GetCurrentUserId()
        {
            try
            {
                // Debug logging
                Console.WriteLine($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
                Console.WriteLine($"User.Identity.Name: {User.Identity?.Name}");
                
                if (User.Identity?.IsAuthenticated == true)
                {
                    // Log all claims for debugging
                    Console.WriteLine("Available claims:");
                    foreach (var claim in User.Claims)
                    {
                        Console.WriteLine($"  {claim.Type}: {claim.Value}");
                    }
                    
                    // Thử nhiều loại claim khác nhau
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                     User.FindFirst("UserId")?.Value ??
                                     User.FindFirst("uid")?.Value ??
                                     User.FindFirst("sub")?.Value ??
                                     User.FindFirst("Id")?.Value;
                    
                    Console.WriteLine($"Found UserId claim: {userIdClaim}");
                    
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                    {
                        Console.WriteLine($"Parsed UserId: {userId}");
                        return userId;
                    }
                    else
                    {
                        Console.WriteLine("Could not parse UserId from claims");
                    }
                }
                else
                {
                    Console.WriteLine("User is not authenticated");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCurrentUserId: {ex.Message}");
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                var addresses = await _context.DiaChis
                    .Where(d => d.IdTaiKhoan == userId)
                    .OrderByDescending(d => d.MacDinh)
                    .ThenBy(d => d.IdDiaChi)
                    .ToListAsync();

                var result = addresses.Select(d => {
                    // Parse fullAddress back to parts: "Detail, Ward, Province"
                    var parts = d.DiaChiChiTiet?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
                    string province = "";
                    string ward = "";
                    string detail = "";
                    
                    if (parts.Count >= 1) province = parts[parts.Count - 1];
                    if (parts.Count >= 2) ward = parts[parts.Count - 2];
                    if (parts.Count >= 3) detail = string.Join(", ", parts.Take(parts.Count - 2));
                    else if (parts.Count == 2) detail = "";
                    else if (parts.Count == 1) detail = "";
                    
                    return new
                    {
                        id = d.IdDiaChi,
                        recipientName = d.HoTenNguoiNhan,
                        phone = d.SoDienThoai,
                        province = province,
                        ward = ward,
                        detailAddress = detail,
                        addressType = d.LoaiDiaChi ?? "home",
                        isDefault = d.MacDinh ?? false,
                        fullAddress = d.DiaChiChiTiet
                    };
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                // Validate input - Ward không bắt buộc
                if (string.IsNullOrWhiteSpace(request.RecipientName) ||
                    string.IsNullOrWhiteSpace(request.Phone) ||
                    string.IsNullOrWhiteSpace(request.Province) ||
                    string.IsNullOrWhiteSpace(request.DetailAddress))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin bắt buộc" });
                }

                // Validate phone number
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^[0-9]{10,11}$"))
                {
                    return Json(new { success = false, message = "Số điện thoại phải có 10-11 chữ số" });
                }

                // If setting as default, remove default from other addresses
                if (request.IsDefault)
                {
                    var existingAddresses = await _context.DiaChis
                        .Where(d => d.IdTaiKhoan == userId && d.MacDinh == true)
                        .ToListAsync();

                    foreach (var addr in existingAddresses)
                    {
                        addr.MacDinh = false;
                    }
                }

                // Create new address - combine address parts, Ward không bắt buộc
                var addressParts = new List<string> { request.DetailAddress };
                if (!string.IsNullOrWhiteSpace(request.Ward))
                    addressParts.Add(request.Ward);
                addressParts.Add(request.Province);
                var fullAddress = string.Join(", ", addressParts);
                
                var newAddress = new DiaChi
                {
                    IdTaiKhoan = userId,
                    HoTenNguoiNhan = request.RecipientName.Trim(),
                    SoDienThoai = request.Phone.Trim(),
                    DiaChiChiTiet = fullAddress,
                    LoaiDiaChi = request.AddressType ?? "home",
                    MacDinh = request.IsDefault
                };

                _context.DiaChis.Add(newAddress);
                await _context.SaveChangesAsync();

                // Parse back the parts for response
                var responseParts = fullAddress.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
                string respProvince = responseParts.Count >= 1 ? responseParts[responseParts.Count - 1] : "";
                string respWard = responseParts.Count >= 2 ? responseParts[responseParts.Count - 2] : "";
                string respDetail = responseParts.Count >= 3 ? string.Join(", ", responseParts.Take(responseParts.Count - 2)) : "";

                return Json(new 
                { 
                    success = true, 
                    message = "Thêm địa chỉ thành công!",
                    data = new
                    {
                        id = newAddress.IdDiaChi,
                        recipientName = newAddress.HoTenNguoiNhan,
                        phone = newAddress.SoDienThoai,
                        province = respProvince,
                        ward = respWard,
                        detailAddress = respDetail,
                        addressType = newAddress.LoaiDiaChi,
                        isDefault = newAddress.MacDinh ?? false,
                        fullAddress = newAddress.DiaChiChiTiet
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressRequest request)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"DeleteAddress called with ID: {request.Id}");
                
                var userId = GetCurrentUserId();
                Console.WriteLine($"Current User ID: {userId}");
                
                if (userId == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                // Tìm địa chỉ trước khi xóa
                var address = await _context.DiaChis
                    .FirstOrDefaultAsync(d => d.IdDiaChi == request.Id && d.IdTaiKhoan == userId);

                Console.WriteLine($"Address found: {address != null}");
                if (address != null)
                {
                    Console.WriteLine($"Address details - ID: {address.IdDiaChi}, UserId: {address.IdTaiKhoan}, Name: {address.HoTenNguoiNhan}");
                }

                if (address == null)
                {
                    Console.WriteLine("Address not found or user doesn't have permission");
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ hoặc bạn không có quyền xóa địa chỉ này" });
                }

                // Trước khi xóa địa chỉ, cập nhật tất cả đơn hàng liên quan để không tham chiếu đến địa chỉ này
                var relatedOrders = await _context.DonHangs
                    .Where(dh => dh.IdDiaChi == request.Id)
                    .ToListAsync();

                foreach (var order in relatedOrders)
                {
                    order.IdDiaChi = null; // Đặt về null thay vì xóa đơn hàng
                }

                Console.WriteLine($"Updated {relatedOrders.Count} orders to remove address reference");

                // Bây giờ có thể xóa địa chỉ an toàn
                _context.DiaChis.Remove(address);
                await _context.SaveChangesAsync();

                Console.WriteLine("Address deleted successfully");
                return Json(new { success = true, message = "Xóa địa chỉ thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting address: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                // Remove default from all addresses
                var allAddresses = await _context.DiaChis
                    .Where(d => d.IdTaiKhoan == userId)
                    .ToListAsync();

                foreach (var addr in allAddresses)
                {
                    addr.MacDinh = (addr.IdDiaChi == id);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã đặt làm địa chỉ mặc định!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAddress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                var addr = await _context.DiaChis
                    .FirstOrDefaultAsync(d => d.IdDiaChi == id && d.IdTaiKhoan == userId);

                if (addr == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
                }

                // Parse fullAddress back to parts
                var parts = addr.DiaChiChiTiet?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new List<string>();
                string province = "";
                string ward = "";
                string detail = "";
                
                if (parts.Count >= 1) province = parts[parts.Count - 1];
                if (parts.Count >= 2) ward = parts[parts.Count - 2];
                if (parts.Count >= 3) detail = string.Join(", ", parts.Take(parts.Count - 2));
                else if (parts.Count == 2) detail = "";
                else if (parts.Count == 1) detail = "";

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = addr.IdDiaChi,
                        recipientName = addr.HoTenNguoiNhan,
                        phone = addr.SoDienThoai,
                        province = province,
                        ward = ward,
                        detailAddress = detail,
                        addressType = addr.LoaiDiaChi ?? "home",
                        isDefault = addr.MacDinh ?? false,
                        fullAddress = addr.DiaChiChiTiet
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi khi lấy thông tin địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateAddressRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }

                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Yêu cầu không hợp lệ" });
                }

                var address = await _context.DiaChis
                    .FirstOrDefaultAsync(d => d.IdDiaChi == request.Id && d.IdTaiKhoan == userId);

                if (address == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ hoặc bạn không có quyền sửa" });
                }

                // Validate input - Ward không bắt buộc
                if (string.IsNullOrWhiteSpace(request.RecipientName) ||
                    string.IsNullOrWhiteSpace(request.Phone) ||
                    string.IsNullOrWhiteSpace(request.Province) ||
                    string.IsNullOrWhiteSpace(request.DetailAddress))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin bắt buộc" });
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Phone, @"^[0-9]{10,11}$"))
                {
                    return Json(new { success = false, message = "Số điện thoại phải có 10-11 chữ số" });
                }

                // If setting as default, unset other defaults
                if (request.IsDefault)
                {
                    var existingDefaults = await _context.DiaChis
                        .Where(d => d.IdTaiKhoan == userId && d.MacDinh == true && d.IdDiaChi != request.Id)
                        .ToListAsync();
                    foreach (var d in existingDefaults)
                    {
                        d.MacDinh = false;
                    }
                }

                // Update fields
                address.HoTenNguoiNhan = request.RecipientName.Trim();
                address.SoDienThoai = request.Phone.Trim();
                // combine to one DiaChiChiTiet - Ward không bắt buộc
                var detailParts = new List<string> { request.DetailAddress.Trim() };
                if (!string.IsNullOrWhiteSpace(request.Ward))
                    detailParts.Add(request.Ward.Trim());
                detailParts.Add(request.Province.Trim());
                address.DiaChiChiTiet = string.Join(", ", detailParts);
                address.LoaiDiaChi = request.AddressType ?? "home";
                address.MacDinh = request.IsDefault;

                await _context.SaveChangesAsync();

                // Parse back for response
                var updatedParts = address.DiaChiChiTiet.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
                string updProvince = updatedParts.Count >= 1 ? updatedParts[updatedParts.Count - 1] : "";
                string updWard = updatedParts.Count >= 2 ? updatedParts[updatedParts.Count - 2] : "";
                string updDetail = updatedParts.Count >= 3 ? string.Join(", ", updatedParts.Take(updatedParts.Count - 2)) : "";

                return Json(new
                {
                    success = true,
                    message = "Cập nhật địa chỉ thành công!",
                    data = new
                    {
                        id = address.IdDiaChi,
                        recipientName = address.HoTenNguoiNhan,
                        phone = address.SoDienThoai,
                        province = updProvince,
                        ward = updWard,
                        detailAddress = updDetail,
                        addressType = address.LoaiDiaChi,
                        isDefault = address.MacDinh ?? false,
                        fullAddress = address.DiaChiChiTiet
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật địa chỉ: " + ex.Message });
            }
        }
    }

    public class CreateAddressRequest
    {
        public string RecipientName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Province { get; set; } = "";
        public string District { get; set; } = "";
        public string Ward { get; set; } = "";
        public string DetailAddress { get; set; } = "";
        public string AddressType { get; set; } = "";
        public bool IsDefault { get; set; }
    }

    // New DTO for update
    public class UpdateAddressRequest
    {
        public int Id { get; set; }
        public string RecipientName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Province { get; set; } = "";
        public string District { get; set; } = "";
        public string Ward { get; set; } = "";
        public string DetailAddress { get; set; } = "";
        public string AddressType { get; set; } = "";
        public bool IsDefault { get; set; }
    }

    public class DeleteAddressRequest
    {
        public int Id { get; set; }
    }
}
