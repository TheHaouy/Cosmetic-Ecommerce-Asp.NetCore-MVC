-- Tăng kích thước cột AnhDanhGia trong bảng DanhGia
-- Để lưu tối đa 5 ảnh với đường dẫn dài

USE LittleFishBeauty;
GO

-- Kiểm tra kích thước hiện tại
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_NAME = 'DanhGia' 
    AND COLUMN_NAME = 'AnhDanhGia';
GO

-- Tăng từ NVARCHAR(XXX) lên NVARCHAR(2000)
-- 2000 ký tự đủ cho 5 ảnh với tên dài nhất
ALTER TABLE DanhGia
ALTER COLUMN AnhDanhGia NVARCHAR(2000) NULL;
GO

-- Kiểm tra lại sau khi thay đổi
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_NAME = 'DanhGia' 
    AND COLUMN_NAME = 'AnhDanhGia';
GO

PRINT 'Đã tăng kích thước cột AnhDanhGia lên NVARCHAR(2000)';
