# HƯỚNG DẪN DEBUG TRANG ĐỊA CHỈ

## ⚠️ Các vấn đề đã sửa:

### 1. ✅ API Controller đã được cập nhật
- `GetAddresses` trả về đầy đủ: province, ward, detailAddress, addressType
- `GetAddress` parse fullAddress thành các phần riêng biệt
- `Create` và `Update` lưu addressType vào database

### 2. ✅ Model DiaChi đã thêm trường LoaiDiaChi
- File: `Data/DiaChi.cs`
- Trường mới: `public string? LoaiDiaChi { get; set; }`

### 3. ✅ DbContext đã có mapping
- File: `Data/LittleFishBeautyContext.cs`  
- Line 270: `entity.Property(e => e.LoaiDiaChi).HasMaxLength(50);`

### 4. ✅ JavaScript đã thêm debug logging
- Các function quan trọng đều có console.log với emoji để dễ theo dõi

## 🔧 BƯỚC THỰC HIỆN:

### Bước 1: Chạy SQL Migration
```sql
-- File: SQL/add_loai_dia_chi_column.sql
-- Chạy script này trong SQL Server Management Studio
```

### Bước 2: Build lại project
```powershell
dotnet build
dotnet run
```

### Bước 3: Mở trang địa chỉ và kiểm tra Console
1. Mở browser (Chrome/Edge)
2. Nhấn F12 để mở Developer Tools
3. Chọn tab Console
4. Vào trang: `/KhachHang/DiaChi`

## 🔍 KIỂM TRA TỪNG CHỨC NĂNG:

### A. Kiểm tra Load Provinces (Tỉnh/Thành phố)
**Khi vào trang, bạn sẽ thấy:**
```
🚀 DiaChi page initialized
📍 Step 1: Loading provinces...
🔄 Loading provinces from: https://www.tinhthanhpho.com/api/v1/new-provinces?limit=100
✅ Provinces loaded: XX provinces (sau sáp nhập 01/07/2025)
📍 Sample province: {code: "...", name: "...", type: "..."}
🔧 Setting up autocomplete for province: province provinceDropdown
✅ Autocomplete elements found for province
```

**Nếu thấy lỗi:**
- ❌ Missing elements → Kiểm tra HTML có input#province và div#provinceDropdown
- ❌ API error → Kiểm tra kết nối internet hoặc API tinhthanhpho.com

### B. Kiểm tra Autocomplete Tỉnh/Thành phố
**Khi gõ vào ô "Tỉnh/Thành phố":**
```
Filtering provinces, total: XX, search: ha
Filtered results: Y
```

**Khi click chọn tỉnh:**
```
🖱️ Selected province: Hà Nội, code: 01
📍 Province selected, loading wards for code: 01
🔄 updateCommunesList called with code: 01
📥 Loading communes for province: 01
✅ Communes loaded: ZZZ
✅ Ward input enabled
```

### C. Kiểm tra Load Danh sách Địa chỉ
**Khi trang load:**
```
🔄 Loading addresses from database...
📡 GetAddresses response status: 200
📦 GetAddresses data: {success: true, data: [...]}
Addresses loaded: [...]
```

**Nếu không có địa chỉ, sẽ hiển thị empty state**

### D. Kiểm tra Button Edit
**Khi click vào button Edit hoặc click vào card:**
```
✏️ Opening edit form for address ID: X
📋 Available addresses: Y
```

**Form sẽ mở và fill dữ liệu tự động**

### E. Kiểm tra Button Delete
**Khi click button Xóa:**
```
🗑️ Attempting to delete address ID: X
✅ Delete confirmed, calling API...
```

**Sau khi confirm, sẽ gọi API DELETE**

### F. Kiểm tra Save Address
**Khi click button "Lưu Địa Chỉ":**
```
💾 saveAddressToDatabase called
📝 Form values: {rawRecipient: "...", rawPhone: "...", ...}
```

## 🐛 CÁC LỖI THƯỜNG GẶP:

### 1. Autocomplete không hiện dropdown
**Nguyên nhân:** 
- Chưa load provinces data
- CSS bị conflict
- Input không có id đúng

**Giải pháp:**
- Kiểm tra console log có "✅ Provinces loaded" không
- Kiểm tra element trong Developer Tools (F12 > Elements tab)
- Xem CSS class `.autocomplete-dropdown.show` có được apply không

### 2. Không thể edit địa chỉ
**Nguyên nhân:**
- Chưa có database migration
- API trả về sai format
- JavaScript lỗi khi parse data

**Giải pháp:**
- Chạy SQL migration: `add_loai_dia_chi_column.sql`
- Kiểm tra console log khi click edit
- Xem Network tab (F12) xem API response như thế nào

### 3. Button không hoạt động
**Nguyên nhân:**
- JavaScript error
- Event bị preventDefault
- HTML không đúng

**Giải pháp:**
- Xem tab Console có lỗi JavaScript không
- Kiểm tra `event.stopPropagation()` trong onclick
- Verify HTML structure của buttons

### 4. API trả về 401 Unauthorized
**Nguyên nhân:**
- Chưa đăng nhập
- Session hết hạn

**Giải pháy:**
- Đăng nhập lại
- Kiểm tra Cookie/Session trong Application tab (F12)

## 📊 KIỂM TRA DATABASE:

```sql
-- Kiểm tra xem cột LoaiDiaChi đã tồn tại chưa
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DiaChi' AND COLUMN_NAME = 'LoaiDiaChi';

-- Xem dữ liệu địa chỉ
SELECT TOP 10 * FROM DiaChi;

-- Kiểm tra có địa chỉ nào chưa có LoaiDiaChi không
SELECT COUNT(*) FROM DiaChi WHERE LoaiDiaChi IS NULL;
```

## ✅ CHECKLIST HOÀN THÀNH:

- [ ] Chạy SQL migration `add_loai_dia_chi_column.sql`
- [ ] Build project thành công (`dotnet build`)
- [ ] Run project (`dotnet run`)
- [ ] Mở trang /KhachHang/DiaChi
- [ ] Kiểm tra Console không có error màu đỏ
- [ ] Test autocomplete Tỉnh/Thành phố
- [ ] Test autocomplete Phường/Xã (sau khi chọn tỉnh)
- [ ] Test thêm địa chỉ mới
- [ ] Test edit địa chỉ
- [ ] Test xóa địa chỉ
- [ ] Test set default address

## 📞 HỖ TRỢ:

Nếu vẫn gặp vấn đề, gửi cho tôi:
1. Screenshot Console log (F12)
2. Screenshot Network tab với API call
3. Thông báo lỗi cụ thể
