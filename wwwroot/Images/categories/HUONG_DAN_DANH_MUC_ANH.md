# CẬP NHẬT DANH MỤC NỔI BẬT - HÌNH ẢNH NỀN

## Những thay đổi đã thực hiện:

### 1. **Cập nhật HTML (Index.cshtml)**
   - Thêm thẻ `<img>` với thuộc tính `src` cho mỗi danh mục
   - Mỗi danh mục có đường dẫn ảnh riêng:
     * Chăm sóc da: `/Images/categories/skincare.jpg`
     * Trang điểm: `/Images/categories/makeup.jpg`
     * Chăm sóc tóc: `/Images/categories/haircare.jpg`
     * Chống nắng: `/Images/categories/suncare.jpg`
     * Tinh dầu: `/Images/categories/oils.jpg`
     * Chăm sóc cơ thể: `/Images/categories/bodycare.jpg`
   - Thêm thuộc tính `onerror="this.src='/Images/noimage.jpg'"` để hiển thị ảnh mặc định nếu không tìm thấy ảnh

### 2. **Cập nhật CSS (trangchu.css)**

#### a. Category Image Container:
   - Tăng chiều cao từ `110px` lên `180px` để hiển thị hình ảnh đẹp hơn
   - Thêm `background-color: #f8f9fa` làm màu nền dự phòng
   - `object-fit: cover` đảm bảo ảnh cover toàn bộ container
   - `object-position: center` căn giữa ảnh

#### b. Category Image:
   - Hiển thị `width: 100%` và `height: 100%`
   - Hiệu ứng zoom nhẹ khi hover: `transform: scale(1.08)`
   - Thêm `display: block` để loại bỏ khoảng trắng thừa

#### c. Gradient Overlay:
   - Thay đổi từ gradient chéo (`45deg`) sang gradient dọc (`to bottom`)
   - Giảm opacity ở phần trên (`0.3`) để hiện rõ hình ảnh
   - Tăng opacity ở phần dưới (`0.85`) để text rõ ràng
   - Cấu trúc: Trong suốt ở trên → Đậm dần ở giữa → Rất đậm ở dưới

#### d. Category Content:
   - Căn text xuống dưới với `justify-content: flex-end`
   - Tăng `min-height` lên `70px`
   - Tăng kích thước chữ cho dễ đọc hơn
   - Thêm text-shadow mạnh hơn để text nổi bật trên ảnh

#### e. Responsive Design:
   - **Desktop lớn (>1200px)**: Chiều cao 200px
   - **Tablet (768px)**: Chiều cao 150px
   - **Mobile (576px)**: Chiều cao 130px
   - Điều chỉnh font-size và padding phù hợp với từng kích thước màn hình

### 3. **Thư mục và File**
   - Tạo thư mục: `wwwroot/Images/categories/`
   - Đã sao chép 6 ảnh mẫu từ thư mục products (bạn có thể thay thế sau)
   - File README.txt hướng dẫn về yêu cầu hình ảnh

## Cách sử dụng:

### Để thay thế hình ảnh:
1. Chuẩn bị hình ảnh chất lượng cao (khuyến nghị 400x400px hoặc 600x600px)
2. Đặt tên file theo quy ước:
   - `skincare.jpg` - Chăm sóc da
   - `makeup.jpg` - Trang điểm
   - `haircare.jpg` - Chăm sóc tóc
   - `suncare.jpg` - Chống nắng
   - `oils.jpg` - Tinh dầu
   - `bodycare.jpg` - Chăm sóc cơ thể
3. Copy file vào thư mục `wwwroot/Images/categories/`
4. Refresh trang để xem kết quả

### Lưu ý:
- Hình ảnh nên rõ nét, màu sắc tươi sáng
- Nội dung ảnh liên quan đến danh mục sản phẩm
- File có thể là .jpg hoặc .png
- Nếu không có ảnh, hệ thống sẽ tự động hiển thị ảnh mặc định

## Hiệu ứng đã thêm:

1. **Hover Effect**: Khi di chuột vào danh mục
   - Ảnh zoom nhẹ (scale 1.08)
   - Box shadow tăng lên
   - Card nâng lên (translateY -8px)

2. **Gradient Overlay**: Làm nổi bật text
   - Mỗi danh mục có màu gradient riêng
   - Gradient từ trong suốt → đậm dần để text dễ đọc

3. **Responsive**: Tự động điều chỉnh kích thước theo thiết bị
   - Desktop: Hình lớn, text to
   - Tablet: Hình trung bình
   - Mobile: Hình nhỏ gọn, vừa đủ

## Kết quả:
✅ Mỗi danh mục nổi bật hiện có hình ảnh nền riêng
✅ Hình ảnh hiển thị vừa đúng kích thước, không bị méo
✅ Text vẫn rõ ràng nhờ gradient overlay
✅ Responsive tốt trên mọi thiết bị
✅ Hiệu ứng hover mượt mà, chuyên nghiệp

## Test:
Bạn có thể test ngay bằng cách:
1. Mở trình duyệt: http://localhost:5000 hoặc https://localhost:5001
2. Xem phần "Danh mục sản phẩm nổi bật"
3. Di chuột vào các danh mục để xem hiệu ứng hover
4. Thay đổi kích thước trình duyệt để test responsive
