# Hướng dẫn cấu hình Facebook Messenger Chat Plugin

## Bước 1: Lấy Facebook Page ID

### Cách 1: Từ trang Facebook của bạn
1. Truy cập vào trang Facebook Page của bạn
2. Nhấp vào **About** (Giới thiệu)
3. Cuộn xuống phần **Page Transparency** (Tính minh bạch của trang)
4. Tìm **Page ID** hoặc **Facebook Page ID**

### Cách 2: Từ URL trang
1. Truy cập: `https://www.facebook.com/YOUR_PAGE_NAME`
2. Xem source code trang (Ctrl+U hoặc Right-click → View Page Source)
3. Tìm kiếm "page_id" hoặc "entity_id"

### Cách 3: Sử dụng Facebook Developer Tools
1. Truy cập: https://developers.facebook.com/tools/explorer/
2. Chọn page của bạn từ dropdown
3. Gọi API: `me?fields=id,name`
4. Bạn sẽ nhận được Page ID

### Cách 4: Từ Meta Business Suite
1. Truy cập: https://business.facebook.com/
2. Chọn page của bạn
3. Vào **Settings** → **Page Info**
4. Sao chép **Page ID**

## Bước 2: Cấu hình trong appsettings.json

Mở file `appsettings.json` và tìm section `FacebookMessenger`:

```json
"FacebookMessenger": {
  "PageId": "YOUR_FACEBOOK_PAGE_ID_HERE",
  "Enabled": true,
  "ThemeColor": "#0084ff",
  "GreetingText": "Xin chào! Chúng tôi có thể giúp gì cho bạn?"
}
```

**Thay thế**:
- `YOUR_FACEBOOK_PAGE_ID_HERE` → Page ID bạn vừa lấy được (ví dụ: `123456789012345`)
- `ThemeColor`: Màu sắc của chat widget (hex color code)
- `GreetingText`: Lời chào mặc định

## Bước 3: Cấu hình trên Facebook Page

### 3.1 Bật Messenger cho Page
1. Vào Facebook Page của bạn
2. Nhấp vào **Settings** (Cài đặt)
3. Chọn **Messaging** (Nhắn tin) từ menu bên trái
4. Bật **Allow people to contact my Page privately by showing the Message button**

### 3.2 Thêm Domain vào Whitelist
1. Truy cập: https://business.facebook.com/
2. Chọn page của bạn
3. Vào **Settings** → **Advanced Messaging**
4. Tìm **Whitelisted Domains**
5. Thêm domain website của bạn (ví dụ: `https://yourdomain.com`)

### 3.3 Cấu hình Messenger Chat Plugin
1. Truy cập: https://developers.facebook.com/docs/messenger-platform/discovery/customer-chat-plugin/
2. Chọn **Setup Tool**
3. Chọn Facebook Page của bạn
4. Thiết lập các tùy chọn:
   - **Greeting dialog display**: Chọn show/hide
   - **Greeting dialog delay**: Thời gian delay trước khi hiện (giây)
   - **Theme color**: Màu chủ đạo của chat
   - **Locale**: `vi_VN` (tiếng Việt)

## Bước 4: Kiểm tra

1. Chạy lại ứng dụng
2. Truy cập trang web của bạn
3. Bạn sẽ thấy icon Messenger xuất hiện ở góc dưới bên phải
4. Nhấp vào để kiểm tra chat

## Khắc phục sự cố

### Plugin không hiển thị?

**Kiểm tra:**
1. Page ID có đúng không?
2. `Enabled` có được set = `true` không?
3. Domain có trong whitelist chưa?
4. Page có bật Messenger không?
5. Mở Console (F12) xem có lỗi JavaScript không?

### Plugin hiển thị nhưng không gửi được tin nhắn?

**Kiểm tra:**
1. Đăng nhập Facebook (người dùng phải đăng nhập)
2. Page có trả lời tự động không? (có thể cấu hình trong Page Settings)
3. Domain có trong whitelist không?

### Muốn tắt tạm thời?

Đặt `"Enabled": false` trong `appsettings.json`

## Tùy chỉnh thêm

### Thay đổi màu sắc
Sửa `ThemeColor` trong `appsettings.json`:
```json
"ThemeColor": "#ff6b6b"  // Màu đỏ cam
```

### Thay đổi vị trí
Thêm CSS tùy chỉnh vào file CSS của bạn:
```css
.fb-customerchat {
    right: 20px !important;
    bottom: 20px !important;
}
```

### Thay đổi ngôn ngữ
Trong file `_Layout_KhachHang.cshtml`, tìm dòng:
```javascript
js.src = 'https://connect.facebook.net/vi_VN/sdk/xfbml.customerchat.js';
```

Thay `vi_VN` bằng:
- `en_US` - Tiếng Anh
- `ja_JP` - Tiếng Nhật
- `ko_KR` - Tiếng Hàn
- v.v...

## Lưu ý quan trọng

1. **Domain Whitelist**: PHẢI thêm domain vào whitelist, nếu không plugin sẽ không hoạt động
2. **HTTPS**: Nên dùng HTTPS cho website để tránh vấn đề bảo mật
3. **Privacy Policy**: Facebook yêu cầu có Privacy Policy khi sử dụng Messenger
4. **Response Time**: Nên trả lời tin nhắn nhanh để có rating tốt trên Facebook

## Tài liệu tham khảo

- [Facebook Messenger Platform Documentation](https://developers.facebook.com/docs/messenger-platform/)
- [Customer Chat Plugin Setup](https://developers.facebook.com/docs/messenger-platform/discovery/customer-chat-plugin/)
- [Messenger Platform Best Practices](https://developers.facebook.com/docs/messenger-platform/introduction/best-practices)
