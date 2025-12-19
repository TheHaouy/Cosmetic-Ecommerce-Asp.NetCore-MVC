using System.Text.RegularExpressions;
using Final_VS1.Data;
using Microsoft.EntityFrameworkCore;

namespace Final_VS1.Helpers
{
    public static class SlugHelper
    {
        /// <summary>
        /// Tạo slug từ tiêu đề sản phẩm/danh mục
        /// Ví dụ: "Kem Dưỡng Da Mặt" -> "kem-duong-da-mat"
        /// </summary>
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Sử dụng VietnameseTextHelper để xử lý tiếng Việt
            var slug = VietnameseTextHelper.RemoveDiacritics(text.ToLower().Trim());

            // Loại bỏ các ký tự đặc biệt, chỉ giữ lại chữ cái, số và khoảng trắng
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Thay thế nhiều khoảng trắng bằng một khoảng trắng
            slug = Regex.Replace(slug, @"\s+", " ");

            // Thay thế khoảng trắng bằng dấu gạch ngang
            slug = slug.Replace(" ", "-");

            // Loại bỏ các dấu gạch ngang liên tiếp
            slug = Regex.Replace(slug, @"-+", "-");

            // Loại bỏ dấu gạch ngang ở đầu và cuối
            slug = slug.Trim('-');

            return slug;
        }

        /// <summary>
        /// Tạo slug độc nhất cho sản phẩm
        /// Nếu slug đã tồn tại, thêm số vào cuối
        /// </summary>
        public static async Task<string> GenerateUniqueSlugForProduct(
            LittleFishBeautyContext context, 
            string text, 
            int? excludeProductId = null)
        {
            var baseSlug = GenerateSlug(text);
            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var exists = await context.SanPhams
                    .AnyAsync(sp => sp.Slug == slug && 
                                   (excludeProductId == null || sp.IdSanPham != excludeProductId));
                
                if (!exists)
                    break;

                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Tạo slug độc nhất cho danh mục
        /// Nếu slug đã tồn tại, thêm số vào cuối
        /// </summary>
        public static async Task<string> GenerateUniqueSlugForCategory(
            LittleFishBeautyContext context, 
            string text, 
            int? excludeCategoryId = null)
        {
            var baseSlug = GenerateSlug(text);
            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var exists = await context.DanhMucs
                    .AnyAsync(dm => dm.DuongDanSeo == slug && 
                                   (excludeCategoryId == null || dm.IdDanhMuc != excludeCategoryId));
                
                if (!exists)
                    break;

                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Kiểm tra slug có hợp lệ không
        /// </summary>
        public static bool IsValidSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            // Slug chỉ chứa chữ cái thường, số và dấu gạch ngang
            return Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$");
        }

        /// <summary>
        /// Chuẩn hóa slug (đảm bảo format đúng)
        /// </summary>
        public static string NormalizeSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            // Chuyển về chữ thường
            slug = slug.ToLower().Trim();

            // Loại bỏ các ký tự không hợp lệ
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");

            // Loại bỏ các dấu gạch ngang liên tiếp
            slug = Regex.Replace(slug, @"-+", "-");

            // Loại bỏ dấu gạch ngang ở đầu và cuối
            slug = slug.Trim('-');

            return slug;
        }

        /// <summary>
        /// Tạo slug từ ID và tiêu đề (format: ten-san-pham-id123)
        /// </summary>
        public static string GenerateSlugWithId(string text, int id)
        {
            var baseSlug = GenerateSlug(text);
            return $"{baseSlug}-{id}";
        }

        /// <summary>
        /// Trích xuất ID từ slug có format: ten-san-pham-id123
        /// </summary>
        public static int? ExtractIdFromSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var match = Regex.Match(slug, @"-(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                return id;

            return null;
        }

        /// <summary>
        /// Tạo breadcrumb từ slug (chuyển slug thành text hiển thị)
        /// Ví dụ: "kem-duong-da-mat" -> "Kem Dưỡng Da Mặt"
        /// </summary>
        public static string SlugToDisplayText(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            // Thay dấu gạch ngang bằng khoảng trắng
            var text = slug.Replace("-", " ");

            // Viết hoa chữ cái đầu mỗi từ
            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Tạo URL tuyệt đối từ slug
        /// </summary>
        public static string GetProductUrl(string slug, string baseUrl = "")
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            var url = $"/san-pham/{slug}";
            return string.IsNullOrWhiteSpace(baseUrl) ? url : $"{baseUrl.TrimEnd('/')}{url}";
        }

        /// <summary>
        /// Tạo URL tuyệt đối từ slug danh mục
        /// </summary>
        public static string GetCategoryUrl(string slug, string baseUrl = "")
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            var url = $"/danh-muc/{slug}";
            return string.IsNullOrWhiteSpace(baseUrl) ? url : $"{baseUrl.TrimEnd('/')}{url}";
        }
    }
}
