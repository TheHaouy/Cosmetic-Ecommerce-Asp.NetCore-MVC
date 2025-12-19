using Final_VS1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

namespace Final_VS1.Controllers
{
    public class SitemapController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly IConfiguration _configuration;

        public SitemapController(LittleFishBeautyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy base URL từ configuration hoặc request
                var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? 
                              $"{Request.Scheme}://{Request.Host}";

                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var urlset = new XElement(ns + "urlset");

                // Trang chủ
                urlset.Add(CreateUrlElement(ns, baseUrl, "/", DateTime.Now, "daily", "1.0"));

                // Lấy danh sách sản phẩm có trạng thái active
                var sanPhams = await _context.SanPhams
                    .Where(sp => sp.TrangThai == true && !string.IsNullOrEmpty(sp.Slug))
                    .Select(sp => new { sp.Slug, sp.NgayTao })
                    .ToListAsync();

                foreach (var sp in sanPhams)
                {
                    var url = $"/san-pham/{sp.Slug}";
                    var lastmod = sp.NgayTao ?? DateTime.Now;
                    urlset.Add(CreateUrlElement(ns, baseUrl, url, lastmod, "weekly", "0.8"));
                }

                // Lấy danh sách danh mục
                var danhMucs = await _context.DanhMucs
                    .Where(dm => !string.IsNullOrEmpty(dm.DuongDanSeo))
                    .Select(dm => new { dm.DuongDanSeo })
                    .ToListAsync();

                foreach (var dm in danhMucs)
                {
                    var url = $"/danh-muc/{dm.DuongDanSeo}";
                    urlset.Add(CreateUrlElement(ns, baseUrl, url, DateTime.Now, "daily", "0.9"));
                }

                // Các trang tĩnh khác (nếu có)
                urlset.Add(CreateUrlElement(ns, baseUrl, "/KhachHang/Gioithieu/Index", DateTime.Now, "monthly", "0.7"));
                urlset.Add(CreateUrlElement(ns, baseUrl, "/KhachHang/Lienhe/Index", DateTime.Now, "monthly", "0.7"));

                var document = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    urlset
                );

                var xml = document.ToString();
                return Content(xml, "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, $"Error generating sitemap: {ex.Message}");
            }
        }

        private XElement CreateUrlElement(XNamespace ns, string baseUrl, string relativeUrl, 
            DateTime lastmod, string changefreq, string priority)
        {
            var url = new XElement(ns + "url");
            
            // Đảm bảo URL được format đúng
            var fullUrl = baseUrl.TrimEnd('/') + relativeUrl;
            
            url.Add(new XElement(ns + "loc", fullUrl));
            url.Add(new XElement(ns + "lastmod", lastmod.ToString("yyyy-MM-dd")));
            url.Add(new XElement(ns + "changefreq", changefreq));
            url.Add(new XElement(ns + "priority", priority));

            return url;
        }
    }
}
