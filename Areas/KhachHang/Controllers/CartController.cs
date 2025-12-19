using Microsoft.AspNetCore.Mvc;
using Final_VS1.Areas.KhachHang.Models;
using Final_VS1.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Security.Claims;
using Final_VS1.Helpers;

namespace Final_VS1.Areas.KhachHang.Controllers
{
    // DTO classes cho API responses
    public class VariantAttributeDto
    {
        public string TenThuocTinh { get; set; } = string.Empty;
        public string GiaTri { get; set; } = string.Empty;
    }

    public class ProductVariantDto
    {
        public int IdBienThe { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal GiaBan { get; set; }
        public int SoLuongTonKho { get; set; }
        public List<VariantAttributeDto> ThuocTinhs { get; set; } = new List<VariantAttributeDto>();
    }

    [Area("KhachHang")]
    public class CartController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public CartController(LittleFishBeautyContext context)
        {
            _context = context;
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

        public IActionResult Index()
        {
            // Lấy giỏ hàng từ database nếu user đã đăng nhập, nếu không thì từ session
            var cartItems = GetCartItems();
            return View(cartItems);
        }

        // Phương thức helper để lấy giỏ hàng
        private List<CartItem> GetCartItems()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Nếu đã đăng nhập, lấy từ database
                return GetCartFromDatabase();
            }
            else
            {
                // Nếu chưa đăng nhập, lấy từ session
                return HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            }
        }

        // Lấy giỏ hàng từ database
        private List<CartItem> GetCartFromDatabase()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return new List<CartItem>();

            var gioHang = _context.GioHangs
                .Include(gh => gh.ChiTietGioHangs)
                    .ThenInclude(ct => ct.IdBienTheNavigation)
                        .ThenInclude(bt => bt.IdSanPhamNavigation)
                            .ThenInclude(sp => sp.AnhSanPhams)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang == null) return new List<CartItem>();

            var cartItems = new List<CartItem>();
            foreach (var chiTiet in gioHang.ChiTietGioHangs)
            {
                var sanPham = chiTiet.IdBienTheNavigation?.IdSanPhamNavigation;
                if (sanPham != null)
                {
                    var linkAnh = GetProductMainImage(sanPham.AnhSanPhams);
                    
                    // Lấy biến thể hiện tại với đầy đủ thông tin thuộc tính
                    var currentBienThe = _context.BienTheSanPhams
                        .Include(bt => bt.IdGiaTris)
                            .ThenInclude(gt => gt.IdThuocTinhNavigation)
                        .FirstOrDefault(bt => bt.IdBienThe == chiTiet.IdBienThe);
                    
                    // Lấy tất cả thuộc tính của biến thể hiện tại
                    var thuocTinhs = new Dictionary<string, string>();
                    if (currentBienThe != null && currentBienThe.IdGiaTris.Any())
                    {
                        foreach (var giaTri in currentBienThe.IdGiaTris)
                        {
                            var tenThuocTinh = giaTri.IdThuocTinhNavigation?.TenThuocTinh;
                            var giaThuocTinh = giaTri.GiaTri;
                            
                            if (!string.IsNullOrEmpty(tenThuocTinh) && !string.IsNullOrEmpty(giaThuocTinh))
                            {
                                thuocTinhs[tenThuocTinh] = giaThuocTinh;
                            }
                        }
                    }
                    
                    // Lấy giá gốc
                    var giaGoc = chiTiet.IdBienTheNavigation?.GiaBan ?? 0;
                    
                    // Tính giá khuyến mãi nếu có
                    var khuyenMai = Helpers.PromotionHelper.GetBestPromotionForProduct(_context, sanPham.IdSanPham, sanPham.IdDanhMuc ?? 0).Result;
                    var giaBan = giaGoc;
                    
                    if (khuyenMai != null)
                    {
                        giaBan = Helpers.PromotionHelper.CalculatePromotionPrice(giaGoc, khuyenMai);
                    }
                    
                    cartItems.Add(new CartItem
                    {
                        IdSanPham = sanPham.IdSanPham,
                        IdBienThe = chiTiet.IdBienThe,
                        TenSanPham = sanPham.TenSanPham,
                        TenBienThe = chiTiet.IdBienTheNavigation?.Sku ?? "Mặc định",
                        LinkAnh = linkAnh,
                        Gia = giaBan, // Sử dụng giá sau khuyến mãi
                        SoLuong = chiTiet.SoLuong ?? 1,
                        ThuocTinhs = thuocTinhs
                    });
                }
            }
            return cartItems;
        }

        // Lưu giỏ hàng vào database
        private void SaveCartToDatabase(List<CartItem> cartItems)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            // Tìm hoặc tạo giỏ hàng cho user
            var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    IdTaiKhoan = userId,
                    NgayCapNhat = DateTime.Now
                };
                _context.GioHangs.Add(gioHang);
                _context.SaveChanges();
            }
            else
            {
                // Xóa chi tiết cũ
                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.NgayCapNhat = DateTime.Now;
            }

            // Thêm chi tiết mới
            foreach (var item in cartItems)
            {
                if (item.IdBienThe.HasValue)
                {
                    var chiTiet = new ChiTietGioHang
                    {
                        IdGioHang = gioHang.IdGioHang,
                        IdBienThe = item.IdBienThe,
                        SoLuong = item.SoLuong
                    };
                    _context.ChiTietGioHangs.Add(chiTiet);
                }
            }
            _context.SaveChanges();
        }

        // Đồng bộ giỏ hàng từ session lên database khi user đăng nhập
        public void SyncCartToDatabase()
        {
            if (User.Identity.IsAuthenticated)
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
                if (sessionCart != null && sessionCart.Any())
                {
                    var dbCart = GetCartFromDatabase();
                    
                    // Merge session cart với database cart
                    foreach (var sessionItem in sessionCart)
                    {
                        var existingItem = dbCart.FirstOrDefault(x => 
                            x.TenSanPham == sessionItem.TenSanPham && 
                            x.TenBienThe == sessionItem.TenBienThe);
                            
                        if (existingItem != null)
                        {
                            existingItem.SoLuong += sessionItem.SoLuong;
                        }
                        else
                        {
                            // Tìm IdBienThe dựa trên tên sản phẩm và biến thể
                            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.TenSanPham == sessionItem.TenSanPham);
                            if (sanPham != null)
                            {
                                var bienThe = _context.BienTheSanPhams.FirstOrDefault(bt => bt.IdSanPham == sanPham.IdSanPham);
                                if (bienThe != null)
                                {
                                    sessionItem.IdSanPham = sanPham.IdSanPham;
                                    sessionItem.IdBienThe = bienThe.IdBienThe;
                                    dbCart.Add(sessionItem);
                                }
                            }
                        }
                    }
                    
                    SaveCartToDatabase(dbCart);
                    HttpContext.Session.Remove("Cart"); // Xóa session cart sau khi đã sync
                }
            }
        }

        // Lấy ID của user hiện tại
        private int? GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }
        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            // Kiểm tra đăng nhập trước khi thêm sản phẩm
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { 
                    success = false, 
                    requireLogin = true,
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng",
                    loginUrl = Url.Action("Index", "DangNhap")
                });
            }

            var product = _context.SanPhams
                .Include(sp => sp.AnhSanPhams)
                .FirstOrDefault(sp => sp.IdSanPham == request.id);
                
            if (product == null)
            {
                return Json(new { 
                    success = false, 
                    message = "Sản phẩm không tồn tại" 
                });
            }

            // Kiểm tra sản phẩm có đang hoạt động không
            if (product.TrangThai != true)
            {
                return Json(new { 
                    success = false, 
                    message = "Sản phẩm hiện tại không khả dụng" 
                });
            }

            // Tự động lấy biến thể đầu tiên của sản phẩm (không cần kiểm tra số lượng biến thể)
            var bienThe = _context.BienTheSanPhams.FirstOrDefault(bt => bt.IdSanPham == product.IdSanPham);
            
            if (bienThe == null)
            {
                return Json(new { 
                    success = false, 
                    message = "Sản phẩm hiện tại không có biến thể khả dụng" 
                });
            }

            var linkAnh = GetProductMainImage(product.AnhSanPhams);

            try
            {
                // Lưu vào database vì đã đăng nhập
                AddToCartDatabase(request, product, bienThe, linkAnh);

                var cartCount = GetCartItemCount();
                return Json(new { 
                    success = true, 
                    cartCount,
                    message = "Đã thêm sản phẩm vào giỏ hàng thành công"
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng" 
                });
            }
        }

        private void AddToCartDatabase(AddToCartRequest request, SanPham product, BienTheSanPham? bienThe, string linkAnh)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            // Tìm hoặc tạo giỏ hàng cho user
            var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    IdTaiKhoan = userId,
                    NgayCapNhat = DateTime.Now
                };
                _context.GioHangs.Add(gioHang);
                _context.SaveChanges();
            }

            if (bienThe != null)
            {
                // Kiểm tra xem sản phẩm đã có trong giỏ chưa
                var existingItem = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.IdBienThe == bienThe.IdBienThe);
                
                if (existingItem != null)
                {
                    // Tăng số lượng
                    existingItem.SoLuong = (existingItem.SoLuong ?? 0) + (request.quantity > 0 ? request.quantity : 1);
                }
                else
                {
                    // Thêm mới
                    var chiTiet = new ChiTietGioHang
                    {
                        IdGioHang = gioHang.IdGioHang,
                        IdBienThe = bienThe.IdBienThe,
                        SoLuong = request.quantity > 0 ? request.quantity : 1
                    };
                    _context.ChiTietGioHangs.Add(chiTiet);
                }

                gioHang.NgayCapNhat = DateTime.Now;
                _context.SaveChanges();
            }
        }

        private void AddToCartSession(AddToCartRequest request, SanPham product, BienTheSanPham? bienThe, string linkAnh)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Nếu sản phẩm đã có trong giỏ thì tăng số lượng, nếu chưa thì thêm mới
            var existing = cart.FirstOrDefault(x => x.TenSanPham == product.TenSanPham);
            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    IdSanPham = product.IdSanPham,
                    IdBienThe = bienThe?.IdBienThe,
                    TenSanPham = product.TenSanPham,
                    TenBienThe = bienThe?.Sku ?? "Mặc định",
                    LinkAnh = linkAnh,
                    Gia = bienThe?.GiaBan ?? 0,
                    SoLuong = request.quantity > 0 ? request.quantity : 1
                });
            }
            else
            {
                existing.SoLuong += request.quantity > 0 ? request.quantity : 1;
            }
            
            // Lưu lại giỏ hàng vào Session
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        // Lấy số lượng sản phẩm trong giỏ hàng
        [HttpGet]
        public IActionResult GetCartCount()
        {
            int cartCount = GetCartItemCount();
            return Json(new { success = true, cartCount });
        }

        private int GetCartItemCount()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();
                if (userId == null) return 0;

                var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                    .FirstOrDefault(gh => gh.IdTaiKhoan == userId);
                return gioHang?.ChiTietGioHangs?.Count ?? 0;
            }
            else
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
                return cart.Count;
            }
        }

        // API để đồng bộ giỏ hàng khi user đăng nhập
        [HttpPost]
        public IActionResult SyncCart()
        {
            try
            {
                SyncCartToDatabase();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                RemoveFromCartDatabase(id);
            }
            else
            {
                RemoveFromCartSession(id);
            }

            return Json(new { success = true });
        }

        private void RemoveFromCartDatabase(int bienTheId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                .ThenInclude(ct => ct.IdBienTheNavigation)
                .ThenInclude(bt => bt.IdSanPhamNavigation)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang != null)
            {
                var itemToRemove = gioHang.ChiTietGioHangs
                    .FirstOrDefault(ct => ct.IdBienThe == bienTheId);

                if (itemToRemove != null)
                {
                    _context.ChiTietGioHangs.Remove(itemToRemove);
                    gioHang.NgayCapNhat = DateTime.Now;
                    _context.SaveChanges();
                }
            }
        }

        private void RemoveFromCartSession(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(x => x.IdSanPham == productId);
            
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(id);
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                UpdateQuantityDatabase(id, quantity);
            }
            else
            {
                UpdateQuantitySession(id, quantity);
            }

            return Json(new { success = true });
        }

        private void UpdateQuantityDatabase(int bienTheId, int quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                .ThenInclude(ct => ct.IdBienTheNavigation)
                .ThenInclude(bt => bt.IdSanPhamNavigation)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang != null)
            {
                var itemToUpdate = gioHang.ChiTietGioHangs
                    .FirstOrDefault(ct => ct.IdBienThe == bienTheId);

                if (itemToUpdate != null)
                {
                    itemToUpdate.SoLuong = quantity;
                    gioHang.NgayCapNhat = DateTime.Now;
                    _context.SaveChanges();
                }
            }
        }

        private void UpdateQuantitySession(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(x => x.IdSanPham == productId);
            
            if (itemToUpdate != null)
            {
                itemToUpdate.SoLuong = quantity;
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
        }

        // Xóa toàn bộ giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ClearCartDatabase();
            }
            else
            {
                HttpContext.Session.Remove("Cart");
            }

            return Json(new { success = true });
        }

        private void ClearCartDatabase()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var gioHang = _context.GioHangs.Include(gh => gh.ChiTietGioHangs)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang != null)
            {
                _context.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
                gioHang.NgayCapNhat = DateTime.Now;
                _context.SaveChanges();
            }
        }

        // API lấy các biến thể của sản phẩm
        [HttpGet]
        public IActionResult GetProductVariants(int productId)
        {
            try
            {
                // Lấy TẤT CẢ các biến thể (không filter theo stock)
                var variants = _context.BienTheSanPhams
                    .Include(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .Where(bt => bt.IdSanPham == productId)
                    .Select(bt => new ProductVariantDto
                    {
                        IdBienThe = bt.IdBienThe,
                        Sku = bt.Sku ?? "",
                        GiaBan = bt.GiaBan ?? 0,
                        SoLuongTonKho = bt.SoLuongTonKho ?? 0,
                        ThuocTinhs = bt.IdGiaTris.Select(gt => new VariantAttributeDto
                        {
                            TenThuocTinh = gt.IdThuocTinhNavigation.TenThuocTinh ?? "",
                            GiaTri = gt.GiaTri ?? ""
                        }).ToList()
                    })
                    .ToList();

                Console.WriteLine($"[API] GetProductVariants for product {productId}: Found {variants.Count} variants");
                foreach (var v in variants)
                {
                    Console.WriteLine($"[API] Variant ID: {v.IdBienThe}, SKU: {v.Sku}, Attrs: {v.ThuocTinhs.Count}");
                }

                return Json(new { success = true, variants });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API cập nhật biến thể sản phẩm trong giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartVariant([FromBody] UpdateVariantRequest request)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    UpdateVariantDatabase(request.OldBienTheId, request.NewBienTheId);
                }
                else
                {
                    UpdateVariantSession(request.OldBienTheId, request.NewBienTheId);
                }

                // Lấy thông tin biến thể mới
                var newVariant = _context.BienTheSanPhams
                    .Include(bt => bt.IdSanPhamNavigation)
                        .ThenInclude(sp => sp.AnhSanPhams)
                    .Include(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .FirstOrDefault(bt => bt.IdBienThe == request.NewBienTheId);

                if (newVariant != null)
                {
                    var linkAnh = GetProductMainImage(newVariant.IdSanPhamNavigation?.AnhSanPhams);
                    var thuocTinhs = newVariant.IdGiaTris.Select(gt => new
                    {
                        TenThuocTinh = gt.IdThuocTinhNavigation.TenThuocTinh,
                        GiaTri = gt.GiaTri
                    }).ToList();

                    // Tính giá sau khuyến mãi
                    var originalPrice = newVariant.GiaBan ?? 0;
                    var finalPrice = originalPrice;
                    
                    // Lấy khuyến mãi tốt nhất cho sản phẩm này
                    int idSanPham = newVariant.IdSanPham ?? 0;
                    int idDanhMuc = newVariant.IdSanPhamNavigation?.IdDanhMuc ?? 0;
                    var khuyenMai = PromotionHelper.GetBestPromotionForProduct(_context, idSanPham, idDanhMuc).Result;
                
                    if (khuyenMai != null)
                    {
                        finalPrice = PromotionHelper.CalculatePromotionPrice(originalPrice, khuyenMai);
                        Console.WriteLine($"[UpdateCartVariant] Product {idSanPham}, Original: {originalPrice}, Discounted: {finalPrice}, Promotion: {khuyenMai.TenKhuyenMai}");
                    }

                    return Json(new
                    {
                        success = true,
                        newPrice = finalPrice, // Trả về giá đã giảm
                        originalPrice = originalPrice, // Trả về cả giá gốc để hiển thị nếu cần
                        newImage = linkAnh,
                        sku = newVariant.Sku,
                        thuocTinhs = thuocTinhs,
                        hasPromotion = khuyenMai != null
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy biến thể" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void UpdateVariantDatabase(int oldBienTheId, int newBienTheId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return;

            var gioHang = _context.GioHangs
                .Include(gh => gh.ChiTietGioHangs)
                .FirstOrDefault(gh => gh.IdTaiKhoan == userId);

            if (gioHang != null)
            {
                var itemToUpdate = gioHang.ChiTietGioHangs
                    .FirstOrDefault(ct => ct.IdBienThe == oldBienTheId);

                if (itemToUpdate != null)
                {
                    itemToUpdate.IdBienThe = newBienTheId;
                    gioHang.NgayCapNhat = DateTime.Now;
                    _context.SaveChanges();
                }
            }
        }

        private void UpdateVariantSession(int oldBienTheId, int newBienTheId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(x => x.IdBienThe == oldBienTheId);

            if (itemToUpdate != null)
            {
                var newVariant = _context.BienTheSanPhams
                    .Include(bt => bt.IdSanPhamNavigation)
                        .ThenInclude(sp => sp.AnhSanPhams)
                    .FirstOrDefault(bt => bt.IdBienThe == newBienTheId);

                if (newVariant != null)
                {
                    itemToUpdate.IdBienThe = newBienTheId;
                    itemToUpdate.TenBienThe = newVariant.Sku ?? "Mặc định";
                    itemToUpdate.Gia = newVariant.GiaBan ?? 0;
                    itemToUpdate.LinkAnh = GetProductMainImage(newVariant.IdSanPhamNavigation?.AnhSanPhams);
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                }
            }
        }

        public class AddToCartRequest
        {
            public int id { get; set; }
            public int quantity { get; set; }
        }

        public class UpdateVariantRequest
        {
            public int OldBienTheId { get; set; }
            public int NewBienTheId { get; set; }
        }
    }
}