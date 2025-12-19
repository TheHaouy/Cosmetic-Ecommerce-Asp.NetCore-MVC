# Hướng dẫn cấu hình Sitemap và SEO

## ✅ Files đã được thiết lập:

1. **`Controllers/SitemapController.cs`** - Controller tự động tạo sitemap
2. **`wwwroot/robots.txt`** - File hướng dẫn search engines
3. **`appsettings.json`** - Cấu hình BaseUrl

## 🔧 Cấu hình trước khi deploy:

### 1. Cập nhật BaseUrl trong appsettings.json

Mở file `appsettings.json` và thay đổi:

```json
"SiteSettings": {
  "BaseUrl": "https://beautylittlefish.com"  // ← Thay bằng domain thực của bạn
}
```

### 2. Cập nhật robots.txt

Mở file `wwwroot/robots.txt` và thay đổi dòng cuối:

```
Sitemap: https://beautylittlefish.com/sitemap.xml  # ← Thay bằng domain thực
```

### 3. Test Sitemap trên localhost

Sau khi chạy ứng dụng, truy cập:

- `https://localhost:7048/sitemap.xml`
- `https://localhost:7048/robots.txt`

Kiểm tra xem có hiển thị đúng không.

## 📊 Các URL trong Sitemap:

Sitemap tự động bao gồm:

- ✅ Trang chủ (`/`)
- ✅ Tất cả sản phẩm active (`/san-pham/{slug}`)
- ✅ Tất cả danh mục active (`/danh-muc/{duongdanseo}`)
- ✅ Trang giới thiệu (`/KhachHang/Gioithieu/Index`)
- ✅ Trang liên hệ (`/KhachHang/Lienhe/Index`)

## 🚫 Các URL bị chặn trong robots.txt:

- ❌ Khu vực Admin và Nhân viên
- ❌ Trang đăng nhập, đăng ký
- ❌ Giỏ hàng và thanh toán
- ❌ Quản lý tài khoản
- ❌ Files hệ thống (_.json, _.config)

## 🌐 Submit lên Search Engines:

### Google Search Console:

1. Truy cập: https://search.google.com/search-console
2. Thêm property (domain của bạn)
3. Xác thực ownership
4. Vào "Sitemaps" → Submit: `https://yourdomain.com/sitemap.xml`

### Bing Webmaster Tools:

1. Truy cập: https://www.bing.com/webmasters
2. Thêm site
3. Submit sitemap URL

## 🧪 Kiểm tra Sitemap:

### Công cụ online:

- https://www.xml-sitemaps.com/validate-xml-sitemap.html
- https://support.google.com/webmasters/answer/7451001

### Kiểm tra với curl:

```bash
curl https://yourdomain.com/sitemap.xml
curl https://yourdomain.com/robots.txt
```

## ⚠️ Lưu ý quan trọng:

1. **Domain phải khớp** giữa `appsettings.json` và `robots.txt`
2. **Chỉ sản phẩm/danh mục active** mới xuất hiện trong sitemap
3. **Sitemap tự động cập nhật** mỗi khi có request (không cần regenerate)
4. **Giới hạn 50,000 URLs** - nếu vượt quá cần chia thành nhiều sitemap files

## 🔍 Kiểm tra lỗi:

### Nếu sitemap không load:

```bash
# Kiểm tra route đã được register
# Kiểm tra database connection
# Xem console logs
```

### Nếu Google không crawl:

- Đợi 1-2 ngày sau khi submit
- Kiểm tra robots.txt không chặn Googlebot
- Verify URL trong Google Search Console

## 📈 Monitoring:

Sau khi submit, theo dõi tại Google Search Console:

- **Coverage**: Số trang được index
- **Sitemaps**: Status của sitemap
- **Performance**: Clicks, impressions
- **Mobile Usability**: Responsive issues

Kiểm tra định kỳ hàng tuần để đảm bảo SEO hoạt động tốt.
