-- Thêm cột TraLoiCuaShop và NgayTraLoi vào bảng DanhGia
-- Script này thêm tính năng reply đánh giá cho Shop

USE LittleFishBeauty;
GO

-- Kiểm tra và thêm cột TraLoiCuaShop nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DanhGia]') AND name = 'TraLoiCuaShop')
BEGIN
    ALTER TABLE [dbo].[DanhGia]
    ADD TraLoiCuaShop NVARCHAR(1000) NULL;
    PRINT 'Đã thêm cột TraLoiCuaShop vào bảng DanhGia';
END
ELSE
BEGIN
    PRINT 'Cột TraLoiCuaShop đã tồn tại trong bảng DanhGia';
END
GO

-- Kiểm tra và thêm cột NgayTraLoi nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DanhGia]') AND name = 'NgayTraLoi')
BEGIN
    ALTER TABLE [dbo].[DanhGia]
    ADD NgayTraLoi DATETIME NULL;
    PRINT 'Đã thêm cột NgayTraLoi vào bảng DanhGia';
END
ELSE
BEGIN
    PRINT 'Cột NgayTraLoi đã tồn tại trong bảng DanhGia';
END
GO

-- Kiểm tra kết quả
SELECT TOP 5 
    ID_DanhGia,
    ID_TaiKhoan,
    ID_SanPham,
    SoSao,
    BinhLuan,
    NgayDanhGia,
    TraLoiCuaShop,
    NgayTraLoi
FROM [dbo].[DanhGia]
ORDER BY ID_DanhGia DESC;
GO

PRINT 'Migration hoàn tất! Bảng DanhGia đã được cập nhật với các cột mới để hỗ trợ tính năng reply.';
GO
