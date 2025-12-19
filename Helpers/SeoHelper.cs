using Final_VS1.Data;
using System.Text;

namespace Final_VS1.Helpers
{
    /// <summary>
    /// Helper class để generate SEO Meta Tags và Structured Data
    /// </summary>
    public static class SeoHelper
    {
        public static string CreateMetaDescription(string? text, int maxLength = 200)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Mỹ phẩm chính hãng, chất lượng cao tại LittleFish Beauty";

            // Remove HTML tags if any
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
            
            // Trim whitespace
            text = text.Trim();

            if (text.Length <= maxLength)
                return text;

            // Cắt ở từ gần nhất
            text = text.Substring(0, maxLength);
            var lastSpace = text.LastIndexOf(' ');
            if (lastSpace > 0)
                text = text.Substring(0, lastSpace);

            return text + "...";
        }

        /// <summary>
        /// Generate keywords từ tên sản phẩm và danh mục
        /// </summary>
        public static string GenerateKeywords(SanPham sanPham, DanhMuc? danhMuc)
        {
            var keywords = new List<string>();

            if (!string.IsNullOrEmpty(sanPham.TenSanPham))
            {
                keywords.Add(sanPham.TenSanPham);
            }

            if (danhMuc != null && !string.IsNullOrEmpty(danhMuc.TenDanhMuc))
            {
                keywords.Add(danhMuc.TenDanhMuc);
            }

            keywords.Add("mỹ phẩm");
            keywords.Add("LittleFish Beauty");
            keywords.Add("chính hãng");

            return string.Join(", ", keywords.Distinct());
        }

        /// <summary>
        /// Format giá tiền cho Open Graph
        /// </summary>
        public static string FormatPrice(decimal price)
        {
            return price.ToString("F0");
        }

        /// <summary>
        /// Lấy canonical URL từ slug
        /// </summary>
        public static string GetCanonicalUrl(string baseUrl, string slug)
        {
            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/san-pham/{slug}";
        }

        /// <summary>
        /// Lấy canonical URL cho danh mục
        /// </summary>
        public static string GetCategoryCanonicalUrl(string baseUrl, string categorySlug)
        {
            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/danh-muc/{categorySlug}";
        }

        /// <summary>
        /// Generate Product Structured Data (JSON-LD)
        /// </summary>
        public static string GenerateProductStructuredData(
            SanPham sanPham, 
            string imageUrl, 
            decimal price,
            int soLuongTonKho,
            double? avgRating = null,
            int? reviewCount = null,
            string? url = null)
        {
            var availability = soLuongTonKho > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock";
            
            var structuredData = new StringBuilder();
            structuredData.AppendLine("{");
            structuredData.AppendLine("  \"@context\": \"https://schema.org/\",");
            structuredData.AppendLine("  \"@type\": \"Product\",");
            structuredData.AppendLine($"  \"name\": \"{EscapeJson(sanPham.TenSanPham)}\",");
            
            if (!string.IsNullOrEmpty(imageUrl))
            {
                structuredData.AppendLine($"  \"image\": \"{EscapeJson(imageUrl)}\",");
            }
            
            if (!string.IsNullOrEmpty(sanPham.MoTa))
            {
                structuredData.AppendLine($"  \"description\": \"{EscapeJson(CreateMetaDescription(sanPham.MoTa, 250))}\",");
            }

            structuredData.AppendLine("  \"brand\": {");
            structuredData.AppendLine("    \"@type\": \"Brand\",");
            structuredData.AppendLine("    \"name\": \"LittleFish Beauty\"");
            structuredData.AppendLine("  },");

            if (avgRating.HasValue && reviewCount.HasValue && reviewCount > 0)
            {
                structuredData.AppendLine("  \"aggregateRating\": {");
                structuredData.AppendLine("    \"@type\": \"AggregateRating\",");
                structuredData.AppendLine($"    \"ratingValue\": \"{avgRating.Value:F1}\",");
                structuredData.AppendLine($"    \"reviewCount\": \"{reviewCount.Value}\"");
                structuredData.AppendLine("  },");
            }

            structuredData.AppendLine("  \"offers\": {");
            structuredData.AppendLine("    \"@type\": \"Offer\",");
            structuredData.AppendLine($"    \"url\": \"{EscapeJson(url ?? "")}\",");
            structuredData.AppendLine($"    \"priceCurrency\": \"VND\",");
            structuredData.AppendLine($"    \"price\": \"{price:F0}\",");
            structuredData.AppendLine($"    \"availability\": \"{availability}\",");
            structuredData.AppendLine("    \"priceValidUntil\": \"2025-12-31\"");
            structuredData.AppendLine("  }");
            structuredData.AppendLine("}");

            return structuredData.ToString();
        }

        /// <summary>
        /// Generate Organization Structured Data (JSON-LD)
        /// </summary>
        public static string GenerateOrganizationStructuredData(string baseUrl, string logoUrl)
        {
            return $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""Organization"",
  ""name"": ""LittleFish Beauty"",
  ""url"": ""{baseUrl}"",
  ""logo"": ""{logoUrl}"",
  ""description"": ""Mỹ phẩm chính hãng, chất lượng cao"",
  ""contactPoint"": {{
    ""@type"": ""ContactPoint"",
    ""contactType"": ""Customer Service"",
    ""availableLanguage"": ""Vietnamese""
  }},
  ""sameAs"": []
}}";
        }

        /// <summary>
        /// Generate BreadcrumbList Structured Data (JSON-LD)
        /// </summary>
        public static string GenerateBreadcrumbStructuredData(List<(string Name, string Url)> breadcrumbs, string baseUrl)
        {
            var structuredData = new StringBuilder();
            structuredData.AppendLine("{");
            structuredData.AppendLine("  \"@context\": \"https://schema.org\",");
            structuredData.AppendLine("  \"@type\": \"BreadcrumbList\",");
            structuredData.AppendLine("  \"itemListElement\": [");

            for (int i = 0; i < breadcrumbs.Count; i++)
            {
                var item = breadcrumbs[i];
                var comma = i < breadcrumbs.Count - 1 ? "," : "";
                
                structuredData.AppendLine("    {");
                structuredData.AppendLine("      \"@type\": \"ListItem\",");
                structuredData.AppendLine($"      \"position\": {i + 1},");
                structuredData.AppendLine($"      \"name\": \"{EscapeJson(item.Name)}\",");
                structuredData.AppendLine($"      \"item\": \"{EscapeJson(baseUrl.TrimEnd('/') + item.Url)}\"");
                structuredData.AppendLine($"    }}{comma}");
            }

            structuredData.AppendLine("  ]");
            structuredData.AppendLine("}");

            return structuredData.ToString();
        }

        /// <summary>
        /// Escape JSON strings
        /// </summary>
        private static string EscapeJson(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ")
                .Replace("\t", " ");
        }

        /// <summary>
        /// Lấy absolute URL từ relative URL
        /// </summary>
        public static string GetAbsoluteUrl(string baseUrl, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return baseUrl;

            if (relativePath.StartsWith("http://") || relativePath.StartsWith("https://"))
                return relativePath;

            baseUrl = baseUrl.TrimEnd('/');
            relativePath = relativePath.TrimStart('/');

            return $"{baseUrl}/{relativePath}";
        }
    }
}
