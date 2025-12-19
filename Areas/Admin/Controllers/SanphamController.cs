using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Final_VS1.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class SanphamController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public SanphamController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // Index - Danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var sanPhams = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                .Include(s => s.AnhSanPhams)
                .OrderByDescending(s => s.NgayTao)
                .ToListAsync();

            return View(sanPhams);
        }

        /// <summary>
        /// Chi tiết sản phẩm theo slug - Admin
        /// URL: /admin/san-pham/{slug}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGia)
                .FirstOrDefaultAsync(s => s.Slug == slug);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Trả về view ChiTiet với model
            return View("ChiTiet", sanPham);
        }

        /// <summary>
        /// Chi tiết sản phẩm theo ID (legacy) - Redirect về slug
        /// </summary>
        [HttpGet]
        [Route("admin/sanpham/chitiet/{id:int}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            
            if (sanPham == null)
            {
                return NotFound();
            }

            // Nếu có slug, redirect về URL đẹp
            if (!string.IsNullOrEmpty(sanPham.Slug))
            {
                return RedirectPermanent($"/admin/san-pham/{sanPham.Slug}");
            }

            // Nếu chưa có slug, tự động tạo
            sanPham.Slug = await SlugHelper.GenerateUniqueSlugForProduct(_context, sanPham.TenSanPham!, sanPham.IdSanPham);
            _context.Update(sanPham);
            await _context.SaveChangesAsync();

            return RedirectPermanent($"/admin/san-pham/{sanPham.Slug}");
        }

        /// <summary>
        /// Chỉnh sửa sản phẩm theo slug - Admin
        /// URL: /admin/san-pham/chinh-sua/{slug}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                .Include(s => s.AnhSanPhams)
                .FirstOrDefaultAsync(s => s.Slug == slug);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Load danh mục cho dropdown
            ViewBag.DanhMucs = await _context.DanhMucs
                .Where(d => d.IdDanhMucCha != null)
                .OrderBy(d => d.TenDanhMuc)
                .ToListAsync();

            // Trả về view ThemSua (form chung cho thêm/sửa)
            return View("ThemSua", sanPham);
        }

        /// <summary>
        /// Thêm mới hoặc chỉnh sửa sản phẩm - Form chung
        /// URL: /admin/sanpham/themsua (thêm mới)
        /// URL: /admin/sanpham/themsua/{id} (chỉnh sửa theo ID - legacy)
        /// </summary>
        [HttpGet]
        [Route("admin/sanpham/themsua/{id:int?}")]
        public async Task<IActionResult> ThemSua(int? id)
        {
            if (id == null)
            {
                // Tạo mới - không có slug
                ViewBag.DanhMucs = await _context.DanhMucs
                    .Where(d => d.IdDanhMucCha != null)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToListAsync();
                
                return View(new SanPham());
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            
            if (sanPham == null)
            {
                return NotFound();
            }

            // Redirect về URL slug
            if (!string.IsNullOrEmpty(sanPham.Slug))
            {
                return RedirectPermanent($"/admin/san-pham/chinh-sua/{sanPham.Slug}");
            }

            // Tạo slug nếu chưa có
            sanPham.Slug = await SlugHelper.GenerateUniqueSlugForProduct(_context, sanPham.TenSanPham!, sanPham.IdSanPham);
            _context.Update(sanPham);
            await _context.SaveChangesAsync();

            return RedirectPermanent($"/admin/san-pham/chinh-sua/{sanPham.Slug}");
        }

        /// <summary>
        /// Xử lý form thêm mới sản phẩm (POST - AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPham sanPham, List<IFormFile>? ImageFiles, string? MainImageUrl)
        {
            try
            {
                // Bỏ qua validation cho navigation properties
                ModelState.Remove("IdDanhMucNavigation");
                ModelState.Remove("AnhSanPhams");
                ModelState.Remove("BienTheSanPhams");
                ModelState.Remove("ChiTietDonHangs");
                ModelState.Remove("ChiTietGioHangs");
                ModelState.Remove("DanhGia");
                ModelState.Remove("IdThanhPhans");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                // Validate slug
                if (string.IsNullOrWhiteSpace(sanPham.Slug))
                {
                    return Json(new { success = false, message = "Slug không được để trống!" });
                }

                // Kiểm tra slug đã tồn tại chưa
                var slugExists = await _context.SanPhams.AnyAsync(sp => sp.Slug == sanPham.Slug);
                if (slugExists)
                {
                    return Json(new { success = false, message = "Slug đã tồn tại, vui lòng chọn slug khác!" });
                }

                // Tạo sản phẩm mới - chỉ set các field cơ bản
                var newProduct = new SanPham
                {
                    TenSanPham = sanPham.TenSanPham,
                    MoTa = sanPham.MoTa,
                    IdDanhMuc = sanPham.IdDanhMuc,
                    TrangThai = sanPham.TrangThai ?? true,
                    NgayTao = DateTime.Now,
                    Slug = sanPham.Slug.ToLower().Trim() // Sử dụng slug từ form
                };

                _context.SanPhams.Add(newProduct);
                await _context.SaveChangesAsync();

                // Xử lý upload ảnh
                if (ImageFiles != null && ImageFiles.Any(f => f.Length > 0))
                {
                    await SaveProductImages(newProduct.IdSanPham, ImageFiles, MainImageUrl);
                }

                return Json(new { success = true, message = "Tạo sản phẩm thành công!" });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, message = $"Lỗi database: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xử lý form cập nhật sản phẩm (POST - AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("admin/sanpham/update")]
        public async Task<IActionResult> Update(SanPham sanPham, List<IFormFile>? ImageFiles, List<string>? ExistingImages, string? MainImageUrl)
        {
            try
            {
                // Bỏ qua validation cho navigation properties
                ModelState.Remove("IdDanhMucNavigation");
                ModelState.Remove("AnhSanPhams");
                ModelState.Remove("BienTheSanPhams");
                ModelState.Remove("ChiTietDonHangs");
                ModelState.Remove("ChiTietGioHangs");
                ModelState.Remove("DanhGia");
                ModelState.Remove("IdThanhPhans");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                var existingProduct = await _context.SanPhams
                    .Include(s => s.AnhSanPhams)
                    .FirstOrDefaultAsync(s => s.IdSanPham == sanPham.IdSanPham);

                if (existingProduct == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Validate slug
                if (string.IsNullOrWhiteSpace(sanPham.Slug))
                {
                    return Json(new { success = false, message = "Slug không được để trống!" });
                }

                // Kiểm tra slug đã tồn tại chưa (ngoại trừ sản phẩm hiện tại)
                var slugExists = await _context.SanPhams
                    .AnyAsync(sp => sp.Slug == sanPham.Slug && sp.IdSanPham != sanPham.IdSanPham);
                if (slugExists)
                {
                    return Json(new { success = false, message = "Slug đã tồn tại, vui lòng chọn slug khác!" });
                }

                // Cập nhật các field
                existingProduct.TenSanPham = sanPham.TenSanPham;
                existingProduct.MoTa = sanPham.MoTa;
                existingProduct.IdDanhMuc = sanPham.IdDanhMuc;
                existingProduct.TrangThai = sanPham.TrangThai;
                existingProduct.Slug = sanPham.Slug.ToLower().Trim(); // Cập nhật slug từ form

                // Xử lý ảnh: xóa ảnh không còn trong ExistingImages
                if (ExistingImages != null && ExistingImages.Any())
                {
                    var imagesToRemove = existingProduct.AnhSanPhams
                        .Where(a => !string.IsNullOrEmpty(a.DuongDan) && !ExistingImages.Contains(a.DuongDan))
                        .ToList();
                    
                    foreach (var img in imagesToRemove)
                    {
                        _context.AnhSanPhams.Remove(img);
                    }
                }
                else if (existingProduct.AnhSanPhams.Any())
                {
                    // Nếu không có ExistingImages nhưng có ảnh cũ, xóa tất cả
                    _context.AnhSanPhams.RemoveRange(existingProduct.AnhSanPhams);
                }

                await _context.SaveChangesAsync();

                // Cập nhật ảnh chính bằng SQL trực tiếp (bypass CHECK constraint)
                if (!string.IsNullOrEmpty(MainImageUrl))
                {
                    // Reset tất cả ảnh về NULL trước
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AnhSanPham SET LoaiAnh = NULL WHERE ID_SanPham = {0}", existingProduct.IdSanPham);
                    
                    // Set ảnh chính
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AnhSanPham SET LoaiAnh = N'Chính' WHERE ID_SanPham = {0} AND DuongDan = {1}", 
                        existingProduct.IdSanPham, MainImageUrl);
                }

                // Upload ảnh mới
                if (ImageFiles != null && ImageFiles.Any(f => f.Length > 0))
                {
                    await SaveProductImages(existingProduct.IdSanPham, ImageFiles, MainImageUrl);
                }

                return Json(new { success = true, message = "Cập nhật sản phẩm thành công!" });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, message = $"Lỗi database: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        /// <summary>
        /// Kiểm tra slug có tồn tại không (cho validation AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckSlugExists(string slug, int? productId = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Json(new { exists = true });
            }

            var exists = await _context.SanPhams
                .AnyAsync(sp => sp.Slug == slug && 
                               (productId == null || sp.IdSanPham != productId));

            return Json(new { exists });
        }

        /// <summary>
        /// Lưu ảnh sản phẩm lên Cloudinary
        /// </summary>
        private async Task SaveProductImages(int productId, List<IFormFile> imageFiles, string? mainImageUrl)
        {
            // TODO: Implement upload to Cloudinary
            // Hiện tại sẽ lưu local
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Products");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uploadedUrls = new List<string>();
            
            foreach (var file in imageFiles.Where(f => f.Length > 0))
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/Images/Products/{fileName}";
                uploadedUrls.Add(imageUrl);

                var anhSanPham = new AnhSanPham
                {
                    IdSanPham = productId,
                    DuongDan = imageUrl
                };

                _context.AnhSanPhams.Add(anhSanPham);
            }

            await _context.SaveChangesAsync();
            
            // Set ảnh chính nếu có chỉ định
            if (!string.IsNullOrEmpty(mainImageUrl) && uploadedUrls.Contains(mainImageUrl))
            {
                // Ảnh chính là ảnh vừa upload
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE AnhSanPham SET LoaiAnh = N'Chính' WHERE ID_SanPham = {0} AND DuongDan = {1}", 
                    productId, mainImageUrl);
            }
            else if (uploadedUrls.Any())
            {
                // Kiểm tra xem sản phẩm đã có ảnh chính chưa
                var hasMainImage = await _context.AnhSanPhams
                    .AnyAsync(a => a.IdSanPham == productId && a.LoaiAnh == "Chính");
                
                if (!hasMainImage)
                {
                    // Set ảnh đầu tiên làm ảnh chính
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AnhSanPham SET LoaiAnh = N'Chính' WHERE ID_SanPham = {0} AND DuongDan = {1}", 
                        productId, uploadedUrls.First());
                }
            }
        }

        /// <summary>
        /// Xóa sản phẩm (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var sanPham = await _context.SanPhams
                    .Include(s => s.BienTheSanPhams)
                        .ThenInclude(bt => bt.IdGiaTris)
                    .Include(s => s.AnhSanPhams)
                    .FirstOrDefaultAsync(s => s.IdSanPham == id);
                
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Xóa liên kết giữa biến thể và giá trị thuộc tính trước
                foreach (var bienThe in sanPham.BienTheSanPhams)
                {
                    bienThe.IdGiaTris.Clear();
                }
                
                // Xóa liên kết sản phẩm - thành phần (bảng SanPham_ThanhPhan)
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SanPham_ThanhPhan WHERE ID_SanPham = {0}", id);
                
                // Xóa biến thể và ảnh
                _context.BienTheSanPhams.RemoveRange(sanPham.BienTheSanPhams);
                _context.AnhSanPhams.RemoveRange(sanPham.AnhSanPhams);
                
                // Xóa sản phẩm
                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                
                // Xử lý thông báo lỗi thân thiện hơn
                string friendlyMessage;
                if (innerMessage.Contains("REFERENCE constraint") || innerMessage.Contains("FK__"))
                {
                    friendlyMessage = "Không thể xóa sản phẩm vì đang được sử dụng trong đơn hàng hoặc các dữ liệu liên quan khác. Vui lòng kiểm tra lại.";
                }
                else
                {
                    friendlyMessage = "Có lỗi xảy ra khi xóa sản phẩm. Vui lòng thử lại sau.";
                }
                
                return Json(new { success = false, message = friendlyMessage });
            }
        }

        /// <summary>
        /// Quản lý biến thể sản phẩm
        /// URL: /admin/sanpham/quanlybienthe/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> QuanLyBienThe(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .FirstOrDefaultAsync(s => s.IdSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Lấy danh sách thuộc tính
            var thuocTinhs = await _context.ThuocTinhs
                .Include(t => t.GiaTriThuocTinhs)
                .OrderBy(t => t.TenThuocTinh)
                .ToListAsync();

            ViewBag.SanPham = sanPham;
            ViewBag.ThuocTinhs = thuocTinhs;
            
            // Trả về danh sách biến thể (IEnumerable<BienTheSanPham>)
            return View(sanPham.BienTheSanPhams);
        }

        /// <summary>
        /// Thêm hoặc sửa biến thể sản phẩm
        /// URL: /admin/sanpham/themsuabienthe?idSanPham={id}&idBienThe={id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ThemSuaBienThe(int idSanPham, int? idBienThe = null)
        {
            // Load sản phẩm kèm theo tất cả biến thể để hiển thị danh sách biến thể hiện có
            var sanPham = await _context.SanPhams
                .Include(sp => sp.BienTheSanPhams)
                    .ThenInclude(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                .FirstOrDefaultAsync(sp => sp.IdSanPham == idSanPham);
            
            if (sanPham == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm";
                return RedirectToAction("Index");
            }

            BienTheSanPham bienThe;
            
            if (idBienThe.HasValue)
            {
                // Sửa biến thể
                var existingBienThe = await _context.BienTheSanPhams
                    .Include(bt => bt.IdGiaTris)
                        .ThenInclude(gt => gt.IdThuocTinhNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(bt => bt.IdBienThe == idBienThe.Value);
                
                if (existingBienThe == null || existingBienThe.IdSanPham != idSanPham)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy biến thể";
                    return RedirectToAction("QuanLyBienThe", new { id = idSanPham });
                }
                
                // Đảm bảo IdGiaTris không null
                if (existingBienThe.IdGiaTris == null)
                {
                    existingBienThe.IdGiaTris = new List<GiaTriThuocTinh>();
                }
                
                bienThe = existingBienThe;
            }
            else
            {
                // Thêm mới
                bienThe = new BienTheSanPham
                {
                    IdSanPham = idSanPham,
                    SoLuongTonKho = 0,
                    IdGiaTris = new List<GiaTriThuocTinh>()
                };
            }

            // Lấy danh sách thuộc tính và giá trị
            var thuocTinhs = await _context.ThuocTinhs
                .Include(t => t.GiaTriThuocTinhs)
                .OrderBy(t => t.TenThuocTinh)
                .ToListAsync();

            ViewBag.SanPham = sanPham;
            ViewBag.ThuocTinhs = thuocTinhs ?? new List<ThuocTinh>();
            ViewBag.ExistingVariants = sanPham.BienTheSanPhams?.ToList() ?? new List<BienTheSanPham>();
            
            return View(bienThe);
        }

        /// <summary>
        /// Lưu biến thể sản phẩm (Thêm mới hoặc Cập nhật)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemSuaBienThe(BienTheSanPham model, int[] giaTriIds)
        {
            try
            {
                var sanPham = await _context.SanPhams.FindAsync(model.IdSanPham);
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction("Index");
                }

                // Lấy danh sách giá trị thuộc tính được chọn
                var selectedGiaTriList = new List<GiaTriThuocTinh>();
                if (giaTriIds != null && giaTriIds.Length > 0)
                {
                    selectedGiaTriList = await _context.GiaTriThuocTinhs
                        .Where(g => giaTriIds.Contains(g.IdGiaTri))
                        .ToListAsync();
                }

                if (model.IdBienThe == 0)
                {
                    // Thêm mới biến thể
                    var newBienThe = new BienTheSanPham
                    {
                        IdSanPham = model.IdSanPham,
                        Sku = model.Sku,
                        GiaBan = model.GiaBan,
                        SoLuongTonKho = model.SoLuongTonKho,
                        IdGiaTris = selectedGiaTriList
                    };

                    _context.BienTheSanPhams.Add(newBienThe);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm biến thể thành công!";
                }
                else
                {
                    // Cập nhật biến thể
                    var existingBienThe = await _context.BienTheSanPhams
                        .Include(bt => bt.IdGiaTris)
                        .FirstOrDefaultAsync(bt => bt.IdBienThe == model.IdBienThe);

                    if (existingBienThe == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy biến thể";
                        return RedirectToAction("QuanLyBienThe", new { id = model.IdSanPham });
                    }

                    // Cập nhật thông tin
                    existingBienThe.Sku = model.Sku;
                    existingBienThe.GiaBan = model.GiaBan;
                    existingBienThe.SoLuongTonKho = model.SoLuongTonKho;

                    // Cập nhật giá trị thuộc tính
                    existingBienThe.IdGiaTris.Clear();
                    foreach (var giaTri in selectedGiaTriList)
                    {
                        existingBienThe.IdGiaTris.Add(giaTri);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật biến thể thành công!";
                }

                return RedirectToAction("QuanLyBienThe", new { id = model.IdSanPham });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("ThemSuaBienThe", new { idSanPham = model.IdSanPham, idBienThe = model.IdBienThe > 0 ? model.IdBienThe : (int?)null });
            }
        }

        /// <summary>
        /// Xóa biến thể sản phẩm (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int idBienThe)
        {
            try
            {
                var bienThe = await _context.BienTheSanPhams
                    .Include(bt => bt.IdGiaTris)
                    .FirstOrDefaultAsync(bt => bt.IdBienThe == idBienThe);

                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể" });
                }

                // Kiểm tra xem biến thể có trong đơn hàng không
                var isInOrder = await _context.ChiTietDonHangs
                    .AnyAsync(ct => ct.IdBienThe == idBienThe);
                
                if (isInOrder)
                {
                    return Json(new { success = false, message = "Không thể xóa biến thể đang có trong đơn hàng" });
                }

                // Xóa liên kết với giá trị thuộc tính
                bienThe.IdGiaTris.Clear();
                
                // Xóa biến thể
                _context.BienTheSanPhams.Remove(bienThe);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa biến thể thành công!" });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                
                string friendlyMessage;
                if (innerMessage.Contains("REFERENCE constraint") || innerMessage.Contains("FK__"))
                {
                    friendlyMessage = "Không thể xóa biến thể vì đang được sử dụng trong đơn hàng hoặc dữ liệu liên quan khác.";
                }
                else
                {
                    friendlyMessage = "Có lỗi xảy ra khi xóa biến thể. Vui lòng thử lại sau.";
                }
                
                return Json(new { success = false, message = friendlyMessage });
            }
        }

        /// <summary>
        /// Quản lý thuộc tính sản phẩm
        /// URL: /admin/sanpham/quanlythuoctinh
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> QuanLyThuocTinh()
        {
            var thuocTinhs = await _context.ThuocTinhs
                .Include(t => t.GiaTriThuocTinhs)
                .OrderBy(t => t.TenThuocTinh)
                .ToListAsync();

            return View(thuocTinhs);
        }

        /// <summary>
        /// Thêm nhanh thuộc tính mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttribute(string tenThuocTinh)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenThuocTinh))
                {
                    return Json(new { success = false, message = "Tên thuộc tính không được để trống" });
                }

                // Kiểm tra thuộc tính đã tồn tại chưa
                var existingAttribute = await _context.ThuocTinhs
                    .FirstOrDefaultAsync(t => t.TenThuocTinh.ToLower() == tenThuocTinh.Trim().ToLower());

                if (existingAttribute != null)
                {
                    return Json(new { success = false, message = "Thuộc tính này đã tồn tại" });
                }

                var newAttribute = new ThuocTinh
                {
                    TenThuocTinh = tenThuocTinh.Trim()
                };

                _context.ThuocTinhs.Add(newAttribute);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm thuộc tính thành công", id = newAttribute.IdThuocTinh });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        /// <summary>
        /// Thêm nhanh giá trị cho thuộc tính
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttributeValue(int thuocTinhId, string giaTris)
        {
            try
            {
                if (thuocTinhId <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn thuộc tính" });
                }

                if (string.IsNullOrWhiteSpace(giaTris))
                {
                    return Json(new { success = false, message = "Giá trị không được để trống" });
                }

                var thuocTinh = await _context.ThuocTinhs.FindAsync(thuocTinhId);
                if (thuocTinh == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
                }

                // Tách các giá trị bằng dấu phẩy
                var values = giaTris.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .ToList();

                if (!values.Any())
                {
                    return Json(new { success = false, message = "Không có giá trị hợp lệ để thêm" });
                }

                // Lấy các giá trị hiện có của thuộc tính
                var existingValues = await _context.GiaTriThuocTinhs
                    .Where(g => g.IdThuocTinh == thuocTinhId)
                    .Select(g => g.GiaTri.ToLower())
                    .ToListAsync();

                var addedValues = new List<string>();
                var duplicateValues = new List<string>();

                foreach (var value in values)
                {
                    if (existingValues.Contains(value.ToLower()))
                    {
                        duplicateValues.Add(value);
                    }
                    else
                    {
                        var newValue = new GiaTriThuocTinh
                        {
                            IdThuocTinh = thuocTinhId,
                            GiaTri = value
                        };
                        _context.GiaTriThuocTinhs.Add(newValue);
                        addedValues.Add(value);
                        existingValues.Add(value.ToLower()); // Thêm vào list để tránh trùng trong cùng request
                    }
                }

                if (addedValues.Any())
                {
                    await _context.SaveChangesAsync();
                }

                var message = addedValues.Any()
                    ? $"Đã thêm {addedValues.Count} giá trị: {string.Join(", ", addedValues)}"
                    : "Không có giá trị mới được thêm";

                if (duplicateValues.Any())
                {
                    message += $". Bỏ qua {duplicateValues.Count} giá trị trùng: {string.Join(", ", duplicateValues)}";
                }

                return Json(new { success = addedValues.Any(), message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách thuộc tính (API)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAttributes()
        {
            try
            {
                var thuocTinhs = await _context.ThuocTinhs
                    .Include(t => t.GiaTriThuocTinhs)
                    .OrderBy(t => t.TenThuocTinh)
                    .Select(t => new
                    {
                        id = t.IdThuocTinh,
                        name = t.TenThuocTinh,
                        values = t.GiaTriThuocTinhs.Select(g => new
                        {
                            id = g.IdGiaTri,
                            value = g.GiaTri
                        }).ToList()
                    })
                    .ToListAsync();

                return Json(new { success = true, data = thuocTinhs });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}