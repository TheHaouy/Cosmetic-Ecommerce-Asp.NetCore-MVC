namespace Final_VS1.Models
{
    /// <summary>
    /// ViewModel cho SEO Meta Tags và Open Graph
    /// </summary>
    public class SeoViewModel
    {
        // Basic SEO
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Keywords { get; set; }
        public string? CanonicalUrl { get; set; }

        // Open Graph (Facebook)
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImage { get; set; }
        public string? OgUrl { get; set; }
        public string? OgType { get; set; } = "website"; // website, product, article
        public string? OgSiteName { get; set; } = "LittleFish Beauty";

        // Twitter Card
        public string? TwitterCard { get; set; } = "summary_large_image";
        public string? TwitterTitle { get; set; }
        public string? TwitterDescription { get; set; }
        public string? TwitterImage { get; set; }

        // Product specific (for Open Graph)
        public decimal? Price { get; set; }
        public string? Currency { get; set; } = "VND";
        public string? Availability { get; set; } // in stock, out of stock
        public string? Brand { get; set; } = "LittleFish Beauty";
        public string? Category { get; set; }

        // Additional
        public string? ImageAlt { get; set; }
        public string? Locale { get; set; } = "vi_VN";

        /// <summary>
        /// Helper method to get Open Graph title (fallback to Title)
        /// </summary>
        public string GetOgTitle() => OgTitle ?? Title ?? "LittleFish Beauty";

        /// <summary>
        /// Helper method to get Open Graph description (fallback to Description)
        /// </summary>
        public string GetOgDescription() => OgDescription ?? Description ?? "Mỹ phẩm chính hãng, chất lượng cao";

        /// <summary>
        /// Helper method to get Open Graph image (fallback to default)
        /// </summary>
        public string GetOgImage() => OgImage ?? "/logo.png";

        /// <summary>
        /// Helper method to get Twitter title (fallback to OgTitle then Title)
        /// </summary>
        public string GetTwitterTitle() => TwitterTitle ?? OgTitle ?? Title ?? "LittleFish Beauty";

        /// <summary>
        /// Helper method to get Twitter description (fallback to OgDescription then Description)
        /// </summary>
        public string GetTwitterDescription() => TwitterDescription ?? OgDescription ?? Description ?? "Mỹ phẩm chính hãng, chất lượng cao";

        /// <summary>
        /// Helper method to get Twitter image (fallback to OgImage)
        /// </summary>
        public string GetTwitterImage() => TwitterImage ?? OgImage ?? "/logo.png";

        /// <summary>
        /// Format price for Open Graph (remove decimals)
        /// </summary>
        public string FormatPrice() => Price.HasValue ? Price.Value.ToString("F0") : "0";
    }
}
