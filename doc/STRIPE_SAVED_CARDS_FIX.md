# HƯỚNG DẪN SỬA LỖI STRIPE SAVED PAYMENT METHODS

## 🔍 VẤN ĐỀ PHÁT HIỆN

Khi khách hàng đăng nhập bằng Google (email: tranthehao7431@gmail.com) và thanh toán bằng Stripe lần đầu, thẻ được lưu thành công. Tuy nhiên, lần thanh toán tiếp theo, popup Stripe không hiển thị thẻ đã lưu mà yêu cầu nhập lại.

### Nguyên nhân gốc rễ:

1. **Cột `StripeCustomerId` chưa tồn tại trong database** - Entity Framework không thể lưu/đọc giá trị
2. **Thiếu mapping trong DbContext** - EF Core không biết cột này tồn tại
3. **Cấu hình JavaScript chưa tối ưu** - Payment Element không được config đúng để hiển thị saved cards
4. **Cấu hình PaymentIntent chưa đúng** - `SetupFutureUsage` cần dùng `off_session` thay vì `on_session`

## ✅ GIẢI PHÁP ĐÃ TRIỂN KHAI

### 1. Thêm cột StripeCustomerId vào Database

**File:** `SQL/add_stripe_customer_id.sql`

Script SQL đã được tạo để:
- Thêm cột `StripeCustomerId NVARCHAR(255) NULL` vào bảng `TaiKhoan`
- Tạo index để tìm kiếm nhanh hơn
- Kiểm tra xem cột đã tồn tại chưa trước khi thêm

**Cách chạy:**
```sql
-- Mở SQL Server Management Studio
-- Kết nối đến database LittlefishBeauty
-- Mở file add_stripe_customer_id.sql
-- Nhấn Execute (F5)
```

### 2. Cập nhật DbContext Mapping

**File:** `Data/LittleFishBeautyContext.cs`

Đã thêm mapping cho cột mới trong `modelBuilder.Entity<TaiKhoan>`:
```csharp
entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
```

Dòng này đặt giữa `SoDienThoai` và `TrangThai` để dễ bảo trì.

### 3. Cải thiện StripeController

**File:** `Areas/KhachHang/Controllers/StripeController.cs`

#### a) Cập nhật CreatePaymentIntent:
- **SetupFutureUsage**: Đổi từ `"on_session"` → `"off_session"` 
  - `off_session` cho phép lưu thẻ để dùng cho cả giao dịch trong tương lai khi user không online
  
- **PaymentMethodOptions.Card.SetupFutureUsage**: Thêm config này
  - Đảm bảo thẻ được lưu đúng cách
  
- **AutomaticPaymentMethods.AllowRedirects**: Set `"never"`
  - Tránh redirect không cần thiết
  
- **Logging**: Thêm log để theo dõi việc tạo PaymentIntent

#### b) Cải thiện GetOrCreateStripeCustomer:
- Thêm logging chi tiết cho mọi bước
- **Xác minh customer tồn tại trên Stripe** trước khi dùng
- Nếu StripeCustomerId trong DB nhưng không tồn tại trên Stripe → tạo mới
- Thêm metadata vào Stripe Customer để dễ debug

### 4. Cập nhật JavaScript Configuration

**File:** `wwwroot/js/KhachHang/thanhtoan.js`

#### Thay đổi trong `initializeStripeElements()`:

**TẠI SAO CẦN THAY ĐỔI:**
- ❌ `paymentMethodCreation: 'manual'` - Ngăn Stripe tự động xử lý saved cards
- ✅ Loại bỏ config này để Stripe tự động hiển thị saved payment methods

**CẤU HÌNH MỚI:**
```javascript
// Appearance với theme màu xanh của bạn
const appearance = { 
    theme: 'stripe',
    variables: {
        colorPrimary: '#2d7b2c'
    }
};

// Elements configuration - BỎ paymentMethodCreation
elements = stripe.elements({ 
    appearance, 
    clientSecret: currentStripeClientSecret,
    // KHÔNG cần paymentMethodCreation: 'manual'
});

// Payment Element options
const paymentElementOptions = { 
    layout: {
        type: 'tabs',
        defaultCollapsed: false, // Hiển thị tabs mở rộng
    },
    paymentMethodOrder: ['card'], // Ưu tiên hiển thị card
};
```

## 🚀 CÁCH KIỂM TRA SAU KHI SỬA

### Bước 1: Chạy SQL Script
```powershell
# Mở SSMS và chạy file SQL
# Hoặc dùng command line:
sqlcmd -S localhost -d LittlefishBeauty -i "d:\Haoo-littlefish-beauty\Beauty_LittleFish\SQL\add_stripe_customer_id.sql"
```

### Bước 2: Kiểm tra Database
```sql
-- Xác nhận cột đã được tạo
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TaiKhoan' 
AND COLUMN_NAME = 'StripeCustomerId';

-- Xem dữ liệu hiện tại
SELECT IdTaiKhoan, Email, HoTen, StripeCustomerId
FROM TaiKhoan
WHERE Email = 'tranthehao7431@gmail.com';
```

### Bước 3: Rebuild Application
```powershell
# Trong thư mục project
dotnet clean
dotnet build
```

### Bước 4: Chạy Application
```powershell
dotnet run
```

### Bước 5: Test Flow Hoàn Chỉnh

#### 🧪 Test Case 1: Lần thanh toán đầu tiên
1. Đăng nhập bằng Google: `tranthehao7431@gmail.com`
2. Thêm sản phẩm vào giỏ hàng
3. Chọn "Thanh toán"
4. Chọn phương thức "Stripe"
5. **Quan sát Console Log** (F12 → Console):
   ```
   [Stripe] Creating new Stripe Customer for user X with email tranthehao7431@gmail.com
   [Stripe] Created new customer: cus_xxxxxxxxxxxxx
   [Stripe] Saved customer ID to database for user X
   [Stripe] PaymentIntent created: pi_xxxxx for Customer: cus_xxxxx
   ```
6. Nhập thông tin thẻ test:
   - Card: `4242 4242 4242 4242`
   - Expiry: `12/34`
   - CVC: `123`
7. Chọn **Save card for future purchases** (checkbox)
8. Hoàn tất thanh toán

#### 🧪 Test Case 2: Lần thanh toán thứ hai (QUAN TRỌNG)
1. Đăng xuất và đăng nhập lại bằng cùng tài khoản Google
2. Thêm sản phẩm khác vào giỏ hàng
3. Chọn "Thanh toán"
4. Chọn phương thức "Stripe"
5. **Quan sát Console Log**:
   ```
   [Stripe] Found existing Stripe Customer ID for user X: cus_xxxxxxxxxxxxx
   [Stripe] Verified customer exists on Stripe: cus_xxxxxxxxxxxxx
   [Stripe] PaymentIntent created: pi_xxxxx for Customer: cus_xxxxx
   ```
6. **QUAN SÁT POPUP STRIPE:**
   - ✅ Phải hiển thị tab "Saved payment methods" hoặc "Payment method"
   - ✅ Thẻ `•••• 4242` phải xuất hiện sẵn
   - ✅ Có nút "Use a different card" để nhập thẻ mới
7. Chọn thẻ đã lưu và hoàn tất thanh toán

### Bước 6: Kiểm tra Database
```sql
-- Sau Test Case 1, kiểm tra StripeCustomerId đã được lưu
SELECT IdTaiKhoan, Email, HoTen, StripeCustomerId
FROM TaiKhoan
WHERE Email = 'tranthehao7431@gmail.com';

-- Kết quả mong đợi:
-- StripeCustomerId = 'cus_xxxxxxxxxxxxx' (không NULL)
```

### Bước 7: Kiểm tra trên Stripe Dashboard
1. Truy cập: https://dashboard.stripe.com/test/customers
2. Tìm customer với email `tranthehao7431@gmail.com`
3. Vào chi tiết customer
4. Tab "Payment methods" phải hiển thị thẻ đã lưu

## 🔧 TROUBLESHOOTING

### Vấn đề 1: Console log không hiển thị customer ID
**Nguyên nhân:** DbContext chưa được rebuild sau khi thêm mapping
**Giải pháp:**
```powershell
dotnet clean
dotnet build
# Restart application
```

### Vấn đề 2: Stripe popup vẫn không hiển thị saved card
**Kiểm tra:**
1. Verify customer ID trong database có giá trị không NULL
2. Kiểm tra customer tồn tại trên Stripe Dashboard
3. Xem Console log xem có error không
4. Clear browser cache: Ctrl+Shift+Delete

**Debug bổ sung:**
```javascript
// Thêm vào thanhtoan.js sau dòng elements = stripe.elements({...})
console.log('[Stripe Debug] ClientSecret:', currentStripeClientSecret);
console.log('[Stripe Debug] Elements created with customer data');
```

### Vấn đề 3: Lỗi "Column 'StripeCustomerId' does not exist"
**Nguyên nhân:** SQL script chưa được chạy
**Giải pháp:** Chạy lại file `add_stripe_customer_id.sql`

### Vấn đề 4: User có StripeCustomerId nhưng customer không tồn tại trên Stripe
**Nguyên nhân:** Customer đã bị xóa trên Stripe Dashboard
**Giải pháp:** Code đã xử lý - sẽ tự động tạo customer mới

## 📊 SO SÁNH TRƯỚC VÀ SAU

| Aspect | Trước | Sau |
|--------|-------|-----|
| Database Column | ❌ Không có | ✅ Có cột StripeCustomerId |
| DbContext Mapping | ❌ Thiếu | ✅ Đã thêm mapping |
| Customer ID Storage | ❌ Không lưu | ✅ Lưu vào DB |
| SetupFutureUsage | ⚠️ on_session | ✅ off_session |
| Payment Element Config | ⚠️ manual creation | ✅ Auto với saved cards |
| Saved Cards Display | ❌ Không hiển thị | ✅ Hiển thị tự động |
| Logging | ⚠️ Cơ bản | ✅ Chi tiết mọi bước |
| Error Handling | ⚠️ Cơ bản | ✅ Xử lý customer không tồn tại |

## 🎯 KẾT QUẢ MONG ĐỢI

Sau khi áp dụng các thay đổi:

1. ✅ User đăng nhập lần đầu → Nhập thẻ → StripeCustomerId được lưu vào DB
2. ✅ User đăng nhập lần 2 → Chọn Stripe → Thẻ đã lưu hiển thị tự động
3. ✅ User có thể chọn thẻ đã lưu hoặc nhập thẻ mới
4. ✅ Mỗi user chỉ có 1 StripeCustomerId duy nhất
5. ✅ Payment methods được gắn với customer, không bị mất khi đăng xuất/đăng nhập lại

## 📝 GHI CHÚ QUAN TRỌNG

1. **Test Mode vs Live Mode:**
   - Đang dùng `pk_test_xxx` (test mode)
   - Thẻ test: 4242 4242 4242 4242
   - Khi lên production, nhớ đổi sang live keys

2. **Security:**
   - StripeCustomerId là public information, không phải secret
   - Không bao giờ lưu card details vào database của bạn
   - Stripe xử lý tất cả card data

3. **PCI Compliance:**
   - Setup này đảm bảo PCI compliance
   - Card data không đi qua server của bạn
   - Stripe Elements xử lý an toàn

4. **Performance:**
   - Index đã được tạo cho StripeCustomerId
   - Query customer sẽ nhanh hơn

## 🔗 TÀI LIỆU THAM KHẢO

- [Stripe Save Payment Methods](https://stripe.com/docs/payments/save-and-reuse)
- [Stripe Payment Element](https://stripe.com/docs/payments/payment-element)
- [Setup Future Usage](https://stripe.com/docs/payments/save-during-payment)
- [Customer Object](https://stripe.com/docs/api/customers)

---

**Ngày tạo:** 05/12/2025  
**Người thực hiện:** GitHub Copilot  
**Trạng thái:** ✅ Đã triển khai đầy đủ
