using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class DanhMucController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DanhMucController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        // Index - Danh sách danh mục
        public async Task<IActionResult> Index()
        {
            var danhMucs = await _context.DanhMucs
                .Include(d => d.IdDanhMucChaNavigation)
                .Include(d => d.InverseIdDanhMucChaNavigation)
                .OrderBy(d => d.ThuTuHienThi)
                .ToListAsync();

            return View(danhMucs);
        }

        // Tạo mới danh mục
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.DanhMucCha = await _context.DanhMucs
                .Where(d => d.IdDanhMucCha == null)
                .OrderBy(d => d.TenDanhMuc)
                .ToListAsync();
            
            return View();
        }

        // Chi tiết danh mục
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var danhMuc = await _context.DanhMucs
                .Include(d => d.IdDanhMucChaNavigation)
                .Include(d => d.InverseIdDanhMucChaNavigation)
                .Include(d => d.SanPhams)
                .FirstOrDefaultAsync(d => d.IdDanhMuc == id);

            if (danhMuc == null)
            {
                return NotFound();
            }

            // Đếm số sản phẩm
            ViewBag.SoLuongSanPham = danhMuc.SanPhams.Count(s => s.TrangThai == true);

            return View(danhMuc);
        }

        // Chỉnh sửa danh mục
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var danhMuc = await _context.DanhMucs
                .Include(d => d.IdDanhMucChaNavigation)
                .FirstOrDefaultAsync(d => d.IdDanhMuc == id);

            if (danhMuc == null)
            {
                return NotFound();
            }

            // Load danh mục cha cho dropdown
            ViewBag.DanhMucCha = await _context.DanhMucs
                .Where(d => d.IdDanhMucCha == null && d.IdDanhMuc != danhMuc.IdDanhMuc)
                .OrderBy(d => d.TenDanhMuc)
                .ToListAsync();

            return View(danhMuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc danhMuc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(danhMuc);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Tạo danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DanhMucCha = await _context.DanhMucs
                    .Where(d => d.IdDanhMucCha == null)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToListAsync();

                return View(danhMuc);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                ViewBag.DanhMucCha = await _context.DanhMucs
                    .Where(d => d.IdDanhMucCha == null)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToListAsync();
                return View(danhMuc);
            }
        }

        // AJAX Create for modal
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto dto)
        {
            try
            {
                var category = new DanhMuc
                {
                    TenDanhMuc = dto.TenDanhMuc,
                    MoTa = dto.MoTa,
                    AnhDaiDien = dto.AnhDaiDien,
                    IdDanhMucCha = dto.IdDanhMucCha,
                    ThuTuHienThi = 0
                };

                _context.Add(category);
                await _context.SaveChangesAsync();

                string? parentName = null;
                if (dto.IdDanhMucCha.HasValue)
                {
                    var parent = await _context.DanhMucs.FindAsync(dto.IdDanhMucCha.Value);
                    parentName = parent?.TenDanhMuc;
                }

                return Json(new
                {
                    success = true,
                    message = "Thêm danh mục thành công!",
                    data = new
                    {
                        id = category.IdDanhMuc,
                        name = category.TenDanhMuc,
                        description = category.MoTa,
                        image = category.AnhDaiDien,
                        parentId = category.IdDanhMucCha,
                        parentName = parentName,
                        order = category.ThuTuHienThi ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DanhMuc danhMuc)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Update(danhMuc);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DanhMucCha = await _context.DanhMucs
                    .Where(d => d.IdDanhMucCha == null && d.IdDanhMuc != danhMuc.IdDanhMuc)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToListAsync();

                return View(danhMuc);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                ViewBag.DanhMucCha = await _context.DanhMucs
                    .Where(d => d.IdDanhMucCha == null && d.IdDanhMuc != danhMuc.IdDanhMuc)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToListAsync();
                return View(danhMuc);
            }
        }

        // AJAX Update for modal
        [HttpPost]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryUpdateDto dto)
        {
            try
            {
                var category = await _context.DanhMucs.FindAsync(dto.Id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                category.TenDanhMuc = dto.TenDanhMuc;
                category.MoTa = dto.MoTa;
                category.AnhDaiDien = dto.AnhDaiDien;
                category.IdDanhMucCha = dto.IdDanhMucCha;

                _context.Update(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa danh mục
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var danhMuc = await _context.DanhMucs
                    .Include(d => d.InverseIdDanhMucChaNavigation)
                    .Include(d => d.SanPhams)
                    .FirstOrDefaultAsync(d => d.IdDanhMuc == id);

                if (danhMuc == null)
                {
                    return NotFound();
                }

                // Kiểm tra có danh mục con không
                if (danhMuc.InverseIdDanhMucChaNavigation.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục có danh mục con!";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra có sản phẩm không
                if (danhMuc.SanPhams.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục có sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                _context.DanhMucs.Remove(danhMuc);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Upload ảnh danh mục
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn file ảnh." });
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)." });
                }

                // Kiểm tra kích thước file (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Kích thước file không được vượt quá 5MB." });
                }

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);
                
                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/images/categories/{fileName}";
                return Json(new { success = true, imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi upload: {ex.Message}" });
            }
        }

        // Lấy danh sách danh mục cha
        [HttpGet]
        public async Task<IActionResult> GetParentCategories(int? excludeId)
        {
            try
            {
                var query = _context.DanhMucs.Where(d => d.IdDanhMucCha == null);
                
                if (excludeId.HasValue)
                {
                    query = query.Where(d => d.IdDanhMuc != excludeId.Value);
                }

                var categories = await query
                    .OrderBy(d => d.TenDanhMuc)
                    .Select(d => new { d.IdDanhMuc, d.TenDanhMuc })
                    .ToListAsync();

                return Json(categories);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Cập nhật thứ tự hiển thị
        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<CategoryOrderUpdate> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    var category = await _context.DanhMucs.FindAsync(update.IdDanhMuc);
                    if (category != null)
                    {
                        category.ThuTuHienThi = update.ThuTuHienThi;
                        _context.Update(category);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Cập nhật danh mục cha
        [HttpPost]
        public async Task<IActionResult> UpdateParent(int id, int? parentId)
        {
            try
            {
                var category = await _context.DanhMucs.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                category.IdDanhMucCha = parentId;
                _context.Update(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Lấy sản phẩm trong danh mục
        [HttpGet]
        public async Task<IActionResult> GetCategoryProducts(int id)
        {
            try
            {
                var category = await _context.DanhMucs
                    .Include(d => d.SanPhams)
                    .FirstOrDefaultAsync(d => d.IdDanhMuc == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                var products = category.SanPhams.Select(p => new
                {
                    p.IdSanPham,
                    p.TenSanPham,
                    p.TrangThai,
                    SoLuongBienThe = p.BienTheSanPhams.Count
                }).ToList();

                return Json(new 
                { 
                    success = true, 
                    products = products,
                    categoryName = category.TenDanhMuc,
                    categoryDescription = category.MoTa
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // DTO cho update order
    public class CategoryOrderUpdate
    {
        public int IdDanhMuc { get; set; }
        public int ThuTuHienThi { get; set; }
    }

    // DTO cho create category
    public class CategoryCreateDto
    {
        public string? TenDanhMuc { get; set; }
        public string? MoTa { get; set; }
        public string? AnhDaiDien { get; set; }
        public int? IdDanhMucCha { get; set; }
    }

    // DTO cho update category
    public class CategoryUpdateDto
    {
        public int Id { get; set; }
        public string? TenDanhMuc { get; set; }
        public string? MoTa { get; set; }
        public string? AnhDaiDien { get; set; }
        public int? IdDanhMucCha { get; set; }
    }
}
