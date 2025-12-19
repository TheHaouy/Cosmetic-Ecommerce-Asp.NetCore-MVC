-- =============================================
-- QUICK TEST - Dữ liệu đánh giá nhanh
-- Beauty Little Fish Database
-- =============================================

USE [LittleFishBeauty]
GO

-- Xóa dữ liệu cũ (nếu muốn test lại từ đầu)
-- DELETE FROM DanhGia;
-- GO

-- Thêm đánh giá nhanh cho sản phẩm
INSERT INTO DanhGia (ID_SanPham, ID_TaiKhoan, SoSao, BinhLuan, NgayDanhGia)
VALUES 
-- Sản phẩm 1: Đánh giá rất tốt (5 sao chiếm đa số)
(1, 1, 5, N'Sản phẩm tuyệt vời!', GETDATE()),
(1, 2, 5, N'Rất hài lòng!', GETDATE()),
(1, 3, 4, N'Tốt, giá hơi cao', GETDATE()),
(1, 4, 5, N'Chất lượng xuất sắc!', GETDATE()),
(1, 5, 5, N'Sẽ mua lại!', GETDATE()),

-- Sản phẩm 2: Đánh giá tốt
(2, 1, 4, N'Kem dưỡng tốt', GETDATE()),
(2, 2, 5, N'Rất mịn màng', GETDATE()),
(2, 3, 4, N'Ổn áp', GETDATE()),
(2, 4, 5, N'Recommend!', GETDATE()),

-- Sản phẩm 3: Đánh giá trung bình
(3, 1, 3, N'Bình thường', GETDATE()),
(3, 2, 4, N'Tạm ổn', GETDATE()),
(3, 3, 3, N'Không có gì đặc biệt', GETDATE()),

-- Sản phẩm 4: Đánh giá xuất sắc
(4, 1, 5, N'Perfect!', GETDATE()),
(4, 2, 5, N'Amazing product!', GETDATE()),
(4, 3, 5, N'Love it!', GETDATE()),
(4, 4, 4, N'Very good', GETDATE()),
(4, 5, 5, N'Highly recommended!', GETDATE()),
(4, 6, 5, N'Best product!', GETDATE()),

-- Sản phẩm 5: Đánh giá tốt
(5, 1, 4, N'Tốt lắm', GETDATE()),
(5, 2, 5, N'Xuất sắc', GETDATE()),
(5, 3, 4, N'Hài lòng', GETDATE());

GO

-- Kiểm tra kết quả
SELECT 
    sp.TenSanPham,
    COUNT(dg.ID_DanhGia) AS SoLuongDanhGia,
    AVG(CAST(dg.SoSao AS FLOAT)) AS DiemTrungBinh,
    SUM(CASE WHEN dg.SoSao = 5 THEN 1 ELSE 0 END) AS Sao5,
    SUM(CASE WHEN dg.SoSao = 4 THEN 1 ELSE 0 END) AS Sao4,
    SUM(CASE WHEN dg.SoSao = 3 THEN 1 ELSE 0 END) AS Sao3,
    SUM(CASE WHEN dg.SoSao = 2 THEN 1 ELSE 0 END) AS Sao2,
    SUM(CASE WHEN dg.SoSao = 1 THEN 1 ELSE 0 END) AS Sao1
FROM SanPham sp
LEFT JOIN DanhGia dg ON sp.ID_SanPham = dg.ID_SanPham
WHERE dg.ID_DanhGia IS NOT NULL
GROUP BY sp.TenSanPham, sp.ID_SanPham
ORDER BY SoLuongDanhGia DESC;

GO

PRINT '✓ Đã thêm dữ liệu đánh giá thành công!'
GO
