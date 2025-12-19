# 📝 Hướng dẫn điền form thiết lập Google Analytics 4

## Bước 1: Tạo tài khoản Google Analytics

### 1.1. Truy cập Google Analytics

- Mở trình duyệt và truy cập: **https://analytics.google.com/**
- Đăng nhập bằng tài khoản Google của bạn
- Nếu chưa có tài khoản Google, tạo mới tại: **https://accounts.google.com/**

### 1.2. Tạo Account (nếu chưa có)

- Click **"Start measuring"** hoặc **"Admin"** (biểu tượng bánh răng góc trái dưới)
- Click **"Create Account"**
- Điền thông tin:
  - **Account name**: `Beauty Little Fish` (hoặc tên công ty bạn)
  - **Account data sharing settings**: ✅ Tick tất cả (recommend)
- Click **"Next"**

---

## Bước 2: Tạo Property (Thuộc tính)

### 2.1. Thông tin Property

Trên màn hình "Property setup":

#### **Property name** (Tên thuộc tính):

```
Beauty Little Fish Website
```

Hoặc:

```
LittleFish Beauty - Mỹ phẩm
```

#### **Reporting time zone** (Múi giờ báo cáo):

```
(GMT+07:00) Bangkok, Hanoi, Jakarta
```

Tìm kiếm: `Vietnam` hoặc `Hanoi`

#### **Currency** (Đơn vị tiền tệ):

```
Vietnamese Dong (₫)
```

Hoặc tìm: `VND`

- Click **"Next"**

### 2.2. Business Information (Thông tin doanh nghiệp)

#### **Industry category** (Ngành nghề):

Chọn: **"Shopping"** → **"Beauty & Fitness"**
Hoặc: **"Retail & Consumer Goods"** → **"Beauty & Personal Care"**

#### **Business size** (Quy mô):

- Chọn: **"Small (1-10 employees)"** nếu là doanh nghiệp nhỏ
- Hoặc: **"Medium (11-50 employees)"** nếu lớn hơn

#### **How you plan to use Google Analytics** (Mục đích sử dụng):

✅ Tick các mục phù hợp:

- ✅ **Examine user behavior** (Phân tích hành vi người dùng)
- ✅ **Measure advertising ROI** (Đo lường hiệu quả quảng cáo)
- ✅ **Get to know your customers** (Hiểu khách hàng)

- Click **"Create"**

### 2.3. Chấp nhận Terms of Service

- Chọn Country: **Vietnam**
- ✅ Tick **"I accept the Google Analytics Terms of Service"**
- ✅ Tick **"I accept the Data Processing Terms"**
- Click **"I Accept"**

---

## Bước 3: Thiết lập Data Stream (Luồng dữ liệu)

### 3.1. Chọn Platform

Trên màn hình "Choose a platform":

- Click nút **"Web"** (biểu tượng màn hình máy tính)

### 3.2. Điền thông tin Web Stream

#### **Website URL** (URL trang web):

```
https://beautylittlefish.com
```

**Hoặc domain thực của bạn, ví dụ:**

```
https://littlefishbeauty.vn
```

**Lưu ý:**

- ✅ Chọn `https://` (có SSL)
- ❌ **KHÔNG** thêm `www.` nếu website không dùng www
- ❌ **KHÔNG** có dấu `/` ở cuối

**Nếu đang test trên localhost:**

```
http://localhost:5245
```

Hoặc:

```
https://localhost:7048
```

#### **Stream name** (Tên luồng):

```
Beauty Little Fish Website
```

Hoặc đơn giản:

```
Website
```

### 3.3. Enhanced Measurement (Đo lường nâng cao)

- ✅ **Bật** toggle button **"Enhanced measurement"**

Tính năng tự động track:

- ✅ Page views (Lượt xem trang)
- ✅ Scrolls (Cuộn trang)
- ✅ Outbound clicks (Click ra ngoài)
- ✅ Site search (Tìm kiếm)
- ✅ Video engagement (Video)
- ✅ File downloads (Tải file)

- Click **"Create stream"**

---

## Bước 4: Lấy Measurement ID

### 4.1. Sau khi tạo stream

Màn hình sẽ hiển thị **"Web stream details"**

### 4.2. Copy Measurement ID

Bạn sẽ thấy:

```
Measurement ID: G-XXXXXXXXXX
```

**Ví dụ thực tế:**

```
G-1A2B3C4D5E
G-ABC123DEF4
G-9876543210
```

- Click vào **icon copy** bên cạnh Measurement ID
- Hoặc **bôi đen và Ctrl+C** để copy

### 4.3. Lưu Measurement ID

**QUAN TRỌNG:** Lưu ID này lại, bạn sẽ cần nó ở bước tiếp theo!

---

## Bước 5: Cấu hình Website

### 5.1. Mở file `appsettings.json`

Trong VS Code, mở file:

```
e:\linhtalinhtinh\BT\HK1_N4\Beauty_LittleFish\appsettings.json
```

### 5.2. Tìm section GoogleAnalytics

Tìm đoạn code:

```json
"GoogleAnalytics": {
  "MeasurementId": "G-XXXXXXXXXX",
  "Enabled": false,
  "Comment": "Get your Measurement ID from https://analytics.google.com - Format: G-XXXXXXXXXX"
}
```

### 5.3. Thay đổi 2 giá trị

**Thay `G-XXXXXXXXXX`** bằng Measurement ID thật của bạn:

```json
"GoogleAnalytics": {
  "MeasurementId": "G-1A2B3C4D5E",  // ← Dán ID của bạn vào đây
  "Enabled": true,                   // ← Đổi từ false thành true
  "Comment": "Get your Measurement ID from https://analytics.google.com - Format: G-XXXXXXXXXX"
}
```

**Ví dụ đầy đủ:**

```json
{
  "ConnectionStrings": {
    "LittleFishBeauty": "..."
  },
  "Logging": { ... },

  "GoogleAnalytics": {
    "MeasurementId": "G-9876543210",
    "Enabled": true
  },

  "AllowedHosts": "*"
}
```

### 5.4. Lưu file

- Nhấn **Ctrl + S** để save
- Hoặc **File → Save**

---

## Bước 6: Test

### 6.1. Chạy website

Mở Terminal trong VS Code:

```bash
dotnet run
```

Hoặc nhấn **F5** để debug.

### 6.2. Mở Browser

Truy cập:

```
http://localhost:5245
```

### 6.3. Kiểm tra Console

1. Nhấn **F12** để mở DevTools
2. Chọn tab **"Console"**
3. Tìm dòng:

```
Google Analytics loaded: G-XXXXXXXXXX
```

Nếu thấy → ✅ **Thành công!**

### 6.4. Kiểm tra trong Google Analytics

1. Quay lại **https://analytics.google.com/**
2. Click **"Reports"** (bên trái)
3. Click **"Realtime"** → **"Overview"**
4. Trong vòng **30 giây**, bạn sẽ thấy:
   - **Users in last 30 minutes: 1**
   - Page view của bạn

Nếu thấy → ✅ **Hoàn tất!**

---

## ⚠️ Troubleshooting (Xử lý lỗi)

### Không thấy data trong Realtime?

**1. Kiểm tra `Enabled: true`**

```json
"GoogleAnalytics": {
  "Enabled": true  // ← Phải là true, không phải false
}
```

**2. Kiểm tra Measurement ID đúng format**

```json
"MeasurementId": "G-1234567890"  // ✅ Đúng
"MeasurementId": "G-XXXXXXXXXX"  // ❌ Sai (chưa thay)
"MeasurementId": "UA-123456-1"   // ❌ Sai (đây là GA cũ)
```

**3. Xóa cache browser**

- Nhấn **Ctrl + Shift + Delete**
- Xóa cache và cookies
- Refresh lại trang (**F5**)

**4. Kiểm tra AdBlock**

- Tắt AdBlock extension
- Hoặc thêm localhost vào whitelist

**5. Kiểm tra Console có lỗi không**
F12 → Console → Xem có message màu đỏ không

**6. Đợi 24-48 giờ**
Đôi khi GA4 cần thời gian để bắt đầu collect data.

---

## 📋 Checklist Hoàn thành

- [ ] Đã tạo Google Analytics account
- [ ] Đã tạo Property với timezone Vietnam
- [ ] Đã tạo Web Data Stream
- [ ] Đã copy Measurement ID (format: G-XXXXXXXXXX)
- [ ] Đã cập nhật `appsettings.json` với ID thật
- [ ] Đã set `Enabled: true`
- [ ] Đã save file
- [ ] Đã test và thấy console log
- [ ] Đã kiểm tra Realtime report có data

---

## 🎓 Tips Pro

### Tip 1: Tạo multiple streams

Bạn có thể tạo nhiều streams cho:

- Production: `https://beautylittlefish.com`
- Staging: `https://staging.beautylittlefish.com`
- Development: `http://localhost:5245`

Mỗi stream sẽ có Measurement ID riêng.

### Tip 2: Sử dụng môi trường khác nhau

```json
// appsettings.json (Production)
"GoogleAnalytics": {
  "MeasurementId": "G-PROD123456",
  "Enabled": true
}

// appsettings.Development.json (Development)
"GoogleAnalytics": {
  "MeasurementId": "G-DEV789012",
  "Enabled": false  // Tắt để không track test data
}
```

### Tip 3: Verify Installation

Cài extension: **"Google Analytics Debugger"** (Chrome)

- Giúp debug GA tracking
- Xem events real-time

---

## 📚 Tài liệu thêm

- **Hướng dẫn chi tiết**: `doc/GOOGLE_ANALYTICS_SETUP.md`
- **E-commerce tracking**: Xem section "E-commerce Events" trong doc
- **Google Analytics Help**: https://support.google.com/analytics/

---

**Thời gian hoàn thành:** 10-15 phút
**Độ khó:** ⭐⭐ Easy-Medium

Chúc bạn setup thành công! 🎉
