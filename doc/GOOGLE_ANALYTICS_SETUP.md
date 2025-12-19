# 📊 Hướng dẫn cấu hình Google Analytics 4 (GA4)

## ✅ Đã thiết lập:

### 1. **Cấu hình trong appsettings.json**

```json
"GoogleAnalytics": {
  "MeasurementId": "G-XXXXXXXXXX",  // ← Thay bằng Measurement ID của bạn
  "Enabled": true                    // false để tắt trong môi trường development
}
```

### 2. **Partial View** - `_GoogleAnalytics.cshtml`

- Tự động load Google Analytics script
- Chỉ chạy khi `Enabled = true`
- Đã tích hợp vào `_Layout_KhachHang.cshtml`

### 3. **Service** - `GoogleAnalyticsService.cs`

- Interface `IGoogleAnalyticsService`
- Quản lý cấu hình GA
- Generate E-commerce tracking scripts

### 4. **E-commerce Helper** - `_GAEcommerceHelper.cshtml`

- Helper functions cho tracking events
- Hỗ trợ: view_item, add_to_cart, purchase, begin_checkout

---

## 🚀 Cách lấy Google Analytics Measurement ID:

### Bước 1: Tạo tài khoản Google Analytics

1. Truy cập: https://analytics.google.com/
2. Đăng nhập bằng Google account
3. Click **"Start measuring"** (hoặc "Admin" nếu đã có account)

### Bước 2: Tạo Property mới

1. Click **"Create Property"**
2. Nhập thông tin:
   - **Property name**: Beauty Little Fish
   - **Reporting time zone**: (GMT+07:00) Bangkok, Hanoi, Jakarta
   - **Currency**: Vietnamese Dong (₫)
3. Click **"Next"**

### Bước 3: Cấu hình Business information

1. Chọn **Industry category**: Shopping / Beauty & Fitness
2. Chọn **Business size**: Small (1-10 employees)
3. Chọn mục đích sử dụng (có thể chọn nhiều)
4. Click **"Create"**

### Bước 4: Chấp nhận Terms of Service

- Đọc và chấp nhận điều khoản
- Click **"I Accept"**

### Bước 5: Thiết lập Data Stream

1. Chọn platform: **Web**
2. Nhập thông tin:
   - **Website URL**: https://beautylittlefish.com
   - **Stream name**: Beauty Little Fish Website
3. Click **"Create stream"**

### Bước 6: Lấy Measurement ID

1. Sau khi tạo stream, bạn sẽ thấy **Measurement ID**
2. Format: `G-XXXXXXXXXX` (ví dụ: `G-ABC123DEF4`)
3. **Copy ID này!**

---

## ⚙️ Cấu hình cho website:

### 1. Cập nhật appsettings.json

**Development** (`appsettings.Development.json`):

```json
{
  "GoogleAnalytics": {
    "MeasurementId": "G-XXXXXXXXXX",
    "Enabled": false // ← Tắt trong development để không track test data
  }
}
```

**Production** (`appsettings.json`):

```json
{
  "GoogleAnalytics": {
    "MeasurementId": "G-XXXXXXXXXX", // ← Measurement ID thật của bạn
    "Enabled": true // ← Bật trong production
  }
}
```

### 2. Test trên localhost

1. Tạm thời bật trong Development:
   ```json
   "Enabled": true
   ```
2. Chạy website: `dotnet run`
3. Mở website: `http://localhost:5245`
4. Mở DevTools (F12) → Console
5. Kiểm tra có thấy:
   ```
   Google Analytics loaded: G-XXXXXXXXXX
   ```

### 3. Kiểm tra Real-time trong GA4

1. Vào Google Analytics
2. Click **"Reports"** → **"Realtime"**
3. Truy cập website của bạn
4. Trong vòng 30 giây, bạn sẽ thấy visitor hiển thị!

---

## 📈 Sử dụng E-commerce Tracking:

### Ví dụ 1: Track khi xem sản phẩm

```csharp
// Trong View (Details.cshtml)
@section Scripts {
    <script>
        @Html.Raw(await Html.PartialAsync("_GAEcommerceHelper"))

        // Track view_item event
        @Html.Raw(_GAEcommerceHelper.ViewItem(
            productId: Model.IdSanPham,
            productName: Model.TenSanPham,
            price: Model.GiaBan,
            category: Model.DanhMuc?.TenDanhMuc ?? ""
        ))
    </script>
}
```

### Ví dụ 2: Track khi thêm vào giỏ

```javascript
// Trong JavaScript khi click "Thêm vào giỏ"
function addToCart(productId, productName, price) {
  // Gọi API thêm vào giỏ...

  // Track GA event
  gtag("event", "add_to_cart", {
    currency: "VND",
    value: price,
    items: [
      {
        item_id: productId,
        item_name: productName,
        price: price,
        quantity: 1,
      },
    ],
  });
}
```

### Ví dụ 3: Track khi thanh toán thành công

```csharp
// Trong Controller sau khi order thành công
public IActionResult PaymentSuccess(int orderId)
{
    var order = _context.DonHangs
        .Include(o => o.ChiTietDonHangs)
        .FirstOrDefault(o => o.IdDonHang == orderId);

    // Generate items JSON
    var items = order.ChiTietDonHangs.Select(ct => new {
        item_id = ct.IdBienThe,
        item_name = ct.IdBienTheNavigation?.IdSanPhamNavigation?.TenSanPham,
        price = ct.DonGia,
        quantity = ct.SoLuong
    });

    ViewBag.GATrackPurchase = new {
        transaction_id = order.MaDonHang,
        value = order.TongTien,
        items = items
    };

    return View();
}

// Trong View (PaymentSuccess.cshtml)
@if (ViewBag.GATrackPurchase != null)
{
    <script>
        gtag('event', 'purchase', @Html.Raw(Json.Serialize(ViewBag.GATrackPurchase)));
    </script>
}
```

---

## 📊 Các Event quan trọng cần track:

### E-commerce Standard Events:

| Event               | Khi nào track          | Dữ liệu cần thiết              |
| ------------------- | ---------------------- | ------------------------------ |
| `view_item`         | Xem chi tiết sản phẩm  | item_id, item_name, price      |
| `view_item_list`    | Xem danh sách sản phẩm | items[]                        |
| `add_to_cart`       | Thêm vào giỏ hàng      | item_id, quantity, value       |
| `remove_from_cart`  | Xóa khỏi giỏ           | item_id                        |
| `view_cart`         | Xem giỏ hàng           | value, items[]                 |
| `begin_checkout`    | Bắt đầu thanh toán     | value, items[]                 |
| `add_payment_info`  | Chọn phương thức TT    | payment_type                   |
| `add_shipping_info` | Nhập địa chỉ GH        | shipping_tier                  |
| `purchase`          | **Hoàn tất đơn hàng**  | transaction_id, value, items[] |

### Custom Events:

```javascript
// Track tìm kiếm
gtag("event", "search", {
  search_term: "kem dưỡng da",
});

// Track đăng ký thành viên
gtag("event", "sign_up", {
  method: "email",
});

// Track đăng nhập
gtag("event", "login", {
  method: "email",
});

// Track share
gtag("event", "share", {
  method: "facebook",
  content_type: "product",
  item_id: "123",
});
```

---

## 🔍 Kiểm tra & Debug:

### 1. Chrome DevTools

```
F12 → Console tab
Filter: "gtag" or "analytics"
```

### 2. Google Analytics DebugView

1. Cài extension: **Google Analytics Debugger**
2. Bật extension
3. Refresh website
4. Vào GA4 → **"Configure"** → **"DebugView"**
5. Xem real-time events chi tiết

### 3. GA4 Real-time Report

- **"Reports"** → **"Realtime"**
- Xem users đang online
- Xem events đang xảy ra

### 4. Tag Assistant (Recommend!)

- Cài extension: **Tag Assistant by Google**
- Click icon extension khi đang ở website
- Xem tất cả tags đang chạy

---

## 📈 Reports hữu ích trong GA4:

### 1. E-commerce Reports

- **"Reports"** → **"Monetization"** → **"E-commerce purchases"**
- Xem: Revenue, transactions, item views, cart-to-view rate

### 2. User Acquisition

- **"Reports"** → **"Acquisition"** → **"User acquisition"**
- Xem: Nguồn traffic, medium, campaign

### 3. Engagement

- **"Reports"** → **"Engagement"** → **"Pages and screens"**
- Xem: Trang nào được xem nhiều nhất

### 4. Conversion

- **"Configure"** → **"Events"**
- Đánh dấu events quan trọng là "Conversion"
- Ví dụ: `purchase`, `sign_up`, `add_to_cart`

---

## ⚠️ Lưu ý quan trọng:

### 1. Privacy & GDPR

- ✅ Đã set `anonymize_ip: true` để ẩn IP
- ⚠️ Cần thêm **Cookie Consent Banner** nếu có users EU
- ⚠️ Thêm **Privacy Policy** page giải thích về tracking

### 2. Bot Filtering

- Trong GA4: **"Admin"** → **"Data Streams"** → Click stream
- **"Configure tag settings"** → **"Show all"**
- Bật **"Exclude all hits from known bots and spiders"**

### 3. Cross-domain Tracking

Nếu có nhiều domains (ví dụ: checkout ở subdomain khác):

```javascript
gtag("config", "G-XXXXXXXXXX", {
  linker: {
    domains: ["beautylittlefish.com", "checkout.beautylittlefish.com"],
  },
});
```

### 4. User ID Tracking

Để track logged-in users:

```javascript
gtag("config", "G-XXXXXXXXXX", {
  user_id: "@User.Identity.Name", // Hoặc userId từ session
});
```

### 5. Enhanced Measurement

Trong GA4, tự động track:

- ✅ Page views
- ✅ Scrolls (90% page)
- ✅ Outbound clicks
- ✅ Site search
- ✅ Video engagement
- ✅ File downloads

---

## 🎯 Goals cho E-commerce:

### Setup Conversions:

1. **"Configure"** → **"Events"**
2. Tìm event: `purchase`
3. Toggle **"Mark as conversion"**

### Key Metrics to Monitor:

- **Conversion Rate**: % users mua hàng
- **Average Order Value (AOV)**: Giá trị đơn hàng TB
- **Cart Abandonment Rate**: % bỏ giỏ hàng
- **Product Performance**: Sản phẩm bán chạy

---

## 🔗 Tài nguyên:

- **GA4 Documentation**: https://support.google.com/analytics/
- **E-commerce Events**: https://developers.google.com/analytics/devguides/collection/ga4/ecommerce
- **GA4 Academy**: https://analytics.google.com/analytics/academy/
- **Tag Manager**: https://tagmanager.google.com/ (Advanced)

---

## ✅ Checklist Deploy:

- [ ] Lấy Measurement ID từ GA4
- [ ] Cập nhật `appsettings.json` với ID thật
- [ ] Set `Enabled: false` trong Development
- [ ] Set `Enabled: true` trong Production
- [ ] Test trên localhost (tạm bật Enabled)
- [ ] Deploy lên production
- [ ] Kiểm tra Real-time report trong GA4
- [ ] Setup Conversions cho `purchase` event
- [ ] Track ít nhất 1 tuần trước khi phân tích
- [ ] Thêm Cookie Consent (nếu cần)

**Hoàn thành!** Website của bạn giờ đã có Google Analytics tracking! 🎉
