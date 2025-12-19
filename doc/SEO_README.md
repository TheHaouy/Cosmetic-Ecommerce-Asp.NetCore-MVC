# SEO Configuration - Beauty Little Fish

## Files đã được tạo:

### 1. robots.txt (`wwwroot/robots.txt`)

File này hướng dẫn các search engine bots về các trang nào được phép crawl và trang nào không.

**Cấu hình:**

- ✅ Cho phép: Trang sản phẩm, danh mục, trang khách hàng công khai
- ❌ Chặn: Admin, Nhân viên, đăng nhập, giỏ hàng, thanh toán
- 📍 Sitemap location: /sitemap.xml

### 2. SitemapController.cs (`Controllers/SitemapController.cs`)

Controller tự động tạo sitemap.xml động từ database.

**Tính năng:**

- Tự động lấy tất cả sản phẩm active từ database
- Tự động lấy tất cả danh mục active
- Hỗ trợ SEO-friendly URLs với slug
- Cấu hình lastmod, changefreq, priority cho mỗi URL

**Route:** `GET /sitemap.xml`

### 3. Cấu hình appsettings.json

Đã thêm `SiteSettings:BaseUrl` để cấu hình domain chính.

```json
"SiteSettings": {
  "BaseUrl": "https://yourdomain.com"
}
```

## Cách sử dụng:

### 1. Cập nhật Base URL

Thay đổi URL trong `appsettings.json`:

```json
"SiteSettings": {
  "BaseUrl": "https://beautylittlefish.com"  // Thay bằng domain thực của bạn
}
```

Và trong `wwwroot/robots.txt`:

```
Sitemap: https://beautylittlefish.com/sitemap.xml
```

### 2. Test Sitemap

Sau khi chạy ứng dụng, truy cập:

- `https://localhost:7048/sitemap.xml` (Development)
- `https://yourdomain.com/sitemap.xml` (Production)

### 3. Test robots.txt

Truy cập:

- `https://localhost:7048/robots.txt` (Development)
- `https://yourdomain.com/robots.txt` (Production)

### 4. Submit lên Google Search Console

1. Đăng nhập vào [Google Search Console](https://search.google.com/search-console)
2. Thêm property (website của bạn)
3. Xác thực quyền sở hữu
4. Vào menu "Sitemaps"
5. Nhập URL: `https://yourdomain.com/sitemap.xml`
6. Click "Submit"

### 5. Submit lên Bing Webmaster Tools

1. Đăng nhập vào [Bing Webmaster Tools](https://www.bing.com/webmasters)
2. Thêm site của bạn
3. Vào "Sitemaps"
4. Submit sitemap URL

## Kiểm tra và Debug:

### Kiểm tra Sitemap có lỗi không:

1. Truy cập sitemap.xml trên trình duyệt
2. Hoặc sử dụng công cụ: [XML Sitemap Validator](https://www.xml-sitemaps.com/validate-xml-sitemap.html)

### Kiểm tra robots.txt:

1. Sử dụng [Google's robots.txt Tester](https://support.google.com/webmasters/answer/6062598)
2. Hoặc test thủ công bằng curl:

```bash
curl https://yourdomain.com/robots.txt
```

## Tùy chỉnh thêm:

### Thêm trang tĩnh vào Sitemap:

Mở `Controllers/SitemapController.cs`, thêm vào method `Index()`:

```csharp
urlset.Add(CreateUrlElement(ns, baseUrl, "/about", DateTime.Now, "monthly", "0.7"));
urlset.Add(CreateUrlElement(ns, baseUrl, "/contact", DateTime.Now, "monthly", "0.7"));
```

### Thêm hình ảnh vào Sitemap (Image Sitemap):

```csharp
// Thêm namespace
XNamespace imageNs = "http://www.google.com/schemas/sitemap-image/1.1";

// Trong CreateUrlElement, thêm:
var image = new XElement(imageNs + "image",
    new XElement(imageNs + "loc", imageUrl)
);
url.Add(image);
```

### Chặn thêm URL trong robots.txt:

Mở `wwwroot/robots.txt`, thêm dòng:

```
Disallow: /path-to-block/
```

## Lưu ý quan trọng:

1. **BaseUrl phải khớp với domain thực tế** khi deploy lên production
2. **Sitemap tự động cập nhật** mỗi khi có request, không cần regenerate thủ công
3. **Chỉ hiển thị sản phẩm/danh mục active** (TrangThai = true)
4. **Sitemap có giới hạn 50,000 URLs** - nếu vượt quá, cần chia thành nhiều sitemap files
5. **robots.txt cần đặt ở root** của domain (đã đặt trong wwwroot)

## Monitoring SEO:

### Google Search Console - Kiểm tra:

- Index coverage (có bao nhiêu trang được index)
- Crawl errors
- Sitemap status
- Mobile usability

### Định kỳ kiểm tra:

- Sitemap có lỗi không: Hàng tuần
- Coverage report: Hàng tuần
- Performance (clicks, impressions): Hàng ngày

## Troubleshooting:

### Sitemap không load được?

- Kiểm tra route đã được register chưa
- Kiểm tra database connection
- Check console logs cho errors

### robots.txt trả về 404?

- Đảm bảo file đặt trong `wwwroot/robots.txt`
- Kiểm tra `app.UseStaticFiles()` đã được config đúng

### Google không crawl sitemap?

- Đợi 1-2 ngày sau khi submit
- Kiểm tra robots.txt không chặn Googlebot
- Verify sitemap URL đúng trong Google Search Console

## Tài liệu tham khảo:

- [Google Sitemap Guidelines](https://developers.google.com/search/docs/advanced/sitemaps/overview)
- [robots.txt Specifications](https://developers.google.com/search/docs/advanced/robots/intro)
- [Bing Webmaster Guidelines](https://www.bing.com/webmasters/help/webmaster-guidelines-30fba23a)
