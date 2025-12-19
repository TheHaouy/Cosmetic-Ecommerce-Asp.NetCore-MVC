using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Models;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class ThuoctinhController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public ThuoctinhController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttribute(string tenThuocTinh)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenThuocTinh))
                    return Json(new { success = false, message = "Tên thuộc tính không được để trống" });

                // Validate length
                if (tenThuocTinh.Length > 255)
                    return Json(new { success = false, message = "Tên thuộc tính quá dài (tối đa 255 ký tự)" });

                // Check for suspicious data (anti-forgery token)
                if (tenThuocTinh.StartsWith("CfDJ8"))
                    return Json(new { success = false, message = "Dữ liệu đầu vào không hợp lệ" });

                var cleanName = tenThuocTinh.Trim();

                // Kiểm tra thuộc tính đã tồn tại
                var existing = await _context.ThuocTinhs
                    .FirstOrDefaultAsync(t => t.TenThuocTinh == cleanName);

                if (existing != null)
                    return Json(new { success = false, message = "Thuộc tính đã tồn tại" });

                var thuocTinh = new ThuocTinh
                {
                    TenThuocTinh = cleanName
                };

                _context.ThuocTinhs.Add(thuocTinh);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm thuộc tính thành công",
                    data = new { idThuocTinh = thuocTinh.IdThuocTinh, tenThuocTinh = thuocTinh.TenThuocTinh }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttributeValue(int idThuocTinh, string giaTri)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"AddAttributeValue called with idThuocTinh: {idThuocTinh}, giaTri: '{giaTri}' (length: {giaTri?.Length ?? 0})");

                // Validate input parameters
                if (idThuocTinh <= 0)
                    return Json(new { success = false, message = "ID thuộc tính không hợp lệ" });

                if (string.IsNullOrWhiteSpace(giaTri))
                    return Json(new { success = false, message = "Giá trị không được để trống" });

                // Check if the input looks like an encrypted token (anti-forgery token)
                if (giaTri.StartsWith("CfDJ8") || giaTri.Length > 500)
                {
                    Console.WriteLine($"Suspicious input detected: '{giaTri.Substring(0, Math.Min(50, giaTri.Length))}...'");
                    return Json(new { success = false, message = "Dữ liệu đầu vào không hợp lệ. Vui lòng thử lại." });
                }

                var thuocTinh = await _context.ThuocTinhs.FindAsync(idThuocTinh);
                if (thuocTinh == null)
                    return Json(new { success = false, message = "Thuộc tính không tồn tại" });

                // Xử lý nhiều giá trị cách nhau bằng dấu phẩy
                // Hỗ trợ đầy đủ Unicode và các ký tự đặc biệt
                var values = giaTri.Split(',')
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v)) // Chỉ loại bỏ chuỗi rỗng và khoảng trắng
                    .Select(v => System.Text.RegularExpressions.Regex.Replace(v, @"\s+", " ")) // Chuẩn hóa khoảng trắng
                    .Where(v => v.Length <= 255) // Filter out values that are too long
                    .Distinct(StringComparer.OrdinalIgnoreCase) // So sánh không phân biệt hoa thường
                    .ToList();

                if (!values.Any())
                    return Json(new { success = false, message = "Không có giá trị hợp lệ sau khi xử lý. Kiểm tra lại dữ liệu đầu vào." });

                // Kiểm tra độ dài giá trị (tối đa 255 ký tự mỗi giá trị)
                var tooLongValues = giaTri.Split(',')
                    .Select(v => v.Trim())
                    .Where(v => v.Length > 255)
                    .ToList();

                if (tooLongValues.Any())
                    return Json(new { success = false, message = $"Các giá trị sau quá dài (tối đa 255 ký tự): {string.Join(", ", tooLongValues.Take(3))}" + (tooLongValues.Count > 3 ? "..." : "") });

                var addedValues = new List<string>();
                var addedValuesData = new List<object>();
                var existingValues = new List<string>();

                // Lấy tất cả giá trị hiện có một lần để tránh multiple queries
                // Sử dụng so sánh không phân biệt hoa thường
                var existingGiaTriList = await _context.GiaTriThuocTinhs
                    .Where(g => g.IdThuocTinh == idThuocTinh)
                    .Select(g => g.GiaTri)
                    .ToListAsync();

                foreach (var value in values)
                {
                    // Double check length again before adding to database
                    if (string.IsNullOrWhiteSpace(value) || value.Length > 255)
                        continue;

                    // Kiểm tra giá trị đã tồn tại (không phân biệt hoa thường)
                    if (existingGiaTriList.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                    {
                        existingValues.Add(value);
                        continue;
                    }

                    var giaTriThuocTinh = new GiaTriThuocTinh
                    {
                        IdThuocTinh = idThuocTinh,
                        GiaTri = value
                    };

                    _context.GiaTriThuocTinhs.Add(giaTriThuocTinh);
                    addedValues.Add(value);
                    addedValuesData.Add(new { giaTri = value, giaTriThuocTinh = giaTriThuocTinh });
                }

                if (addedValues.Any())
                {
                    await _context.SaveChangesAsync();

                    // Cập nhật addedValuesData với ID thực tế sau khi save
                    addedValuesData = addedValuesData.Select(item => new
                    {
                        idGiaTri = ((GiaTriThuocTinh)((dynamic)item).giaTriThuocTinh).IdGiaTri,
                        giaTri = (string)((dynamic)item).giaTri
                    }).Cast<object>().ToList();
                }

                var message = "";
                if (addedValues.Any() && existingValues.Any())
                {
                    message = $"Đã thêm {addedValues.Count} giá trị. {existingValues.Count} giá trị đã tồn tại: {string.Join(", ", existingValues)}";
                }
                else if (addedValues.Any())
                {
                    message = $"Đã thêm thành công {addedValues.Count} giá trị";
                }
                else
                {
                    message = $"Tất cả giá trị đã tồn tại: {string.Join(", ", existingValues)}";
                }

                return Json(new
                {
                    success = addedValues.Any(),
                    message = message,
                    data = addedValuesData
                });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttributeValue(int idGiaTri)
        {
            try
            {
                var giaTriThuocTinh = await _context.GiaTriThuocTinhs.FindAsync(idGiaTri);
                if (giaTriThuocTinh == null)
                    return Json(new { success = false, message = "Giá trị không tồn tại" });

                // Kiểm tra xem giá trị có đang được sử dụng trong biến thể nào không
                var isUsed = await _context.BienTheSanPhams
                    .Include(b => b.IdGiaTris)
                    .AnyAsync(b => b.IdGiaTris.Any(g => g.IdGiaTri == idGiaTri));

                if (isUsed)
                    return Json(new { success = false, message = "Không thể xóa giá trị đang được sử dụng trong biến thể" });

                _context.GiaTriThuocTinhs.Remove(giaTriThuocTinh);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa giá trị thành công" });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameAttribute(int idThuocTinh, string tenThuocTinh)
        {
            try
            {
                if (idThuocTinh <= 0)
                    return Json(new { success = false, message = "ID thuộc tính không hợp lệ" });

                if (string.IsNullOrWhiteSpace(tenThuocTinh))
                    return Json(new { success = false, message = "Tên thuộc tính không được để trống" });

                var cleanName = tenThuocTinh.Trim();

                if (cleanName.Length > 255)
                    return Json(new { success = false, message = "Tên thuộc tính quá dài (tối đa 255 ký tự)" });

                // Chặn dữ liệu không hợp lệ trông giống token
                if (cleanName.StartsWith("CfDJ8"))
                    return Json(new { success = false, message = "Dữ liệu đầu vào không hợp lệ" });

                var thuocTinh = await _context.ThuocTinhs.FindAsync(idThuocTinh);
                if (thuocTinh == null)
                    return Json(new { success = false, message = "Thuộc tính không tồn tại" });

                // Kiểm tra trùng tên (không phân biệt hoa thường) với thuộc tính khác
                var existed = await _context.ThuocTinhs
                    .AnyAsync(t => t.IdThuocTinh != idThuocTinh && t.TenThuocTinh.ToLower() == cleanName.ToLower());

                if (existed)
                    return Json(new { success = false, message = "Tên thuộc tính đã tồn tại" });

                thuocTinh.TenThuocTinh = cleanName;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đổi tên thuộc tính thành công", data = new { idThuocTinh, tenThuocTinh = cleanName } });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttributesWithValues()
        {
            try
            {
                var attributes = await _context.ThuocTinhs
                    .Include(t => t.GiaTriThuocTinhs)
                    .OrderBy(t => t.TenThuocTinh)
                    .Select(t => new
                    {
                        idThuocTinh = t.IdThuocTinh,
                        tenThuocTinh = t.TenThuocTinh,
                        giaTriList = t.GiaTriThuocTinhs.OrderBy(g => g.GiaTri).Select(g => new
                        {
                            idGiaTri = g.IdGiaTri,
                            giaTri = g.GiaTri
                        }).ToList()
                    })
                    .ToListAsync();

                return Json(new { success = true, data = attributes });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }

        /// <summary>
        /// Xóa thuộc tính và tất cả giá trị của nó
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThuocTinh(int id)
        {
            try
            {
                var thuocTinh = await _context.ThuocTinhs
                    .Include(t => t.GiaTriThuocTinhs)
                    .FirstOrDefaultAsync(t => t.IdThuocTinh == id);

                if (thuocTinh == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
                }

                // Kiểm tra xem có giá trị nào đang được sử dụng trong biến thể không
                var usedValueIds = thuocTinh.GiaTriThuocTinhs.Select(g => g.IdGiaTri).ToList();
                var isUsed = await _context.BienTheSanPhams
                    .Include(b => b.IdGiaTris)
                    .AnyAsync(b => b.IdGiaTris.Any(g => usedValueIds.Contains(g.IdGiaTri)));

                if (isUsed)
                {
                    return Json(new { success = false, message = "Không thể xóa thuộc tính đang được sử dụng trong biến thể sản phẩm" });
                }

                // Xóa tất cả giá trị của thuộc tính
                _context.GiaTriThuocTinhs.RemoveRange(thuocTinh.GiaTriThuocTinhs);
                
                // Xóa thuộc tính
                _context.ThuocTinhs.Remove(thuocTinh);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa thuộc tính thành công!" });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra: " + errorMessage });
            }
        }
    }
}
