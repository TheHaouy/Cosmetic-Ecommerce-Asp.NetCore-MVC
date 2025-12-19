-- Script: Thêm cột StripeCustomerId vào bảng TaiKhoan
-- Mục đích: Lưu ID khách hàng từ Stripe để tái sử dụng payment methods

USE [LittlefishBeauty]
GO

-- Kiểm tra xem cột đã tồn tại chưa
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TaiKhoan' 
    AND COLUMN_NAME = 'StripeCustomerId'
)
BEGIN
    -- Thêm cột mới
    ALTER TABLE TaiKhoan
    ADD StripeCustomerId NVARCHAR(255) NULL;
    
    PRINT 'Đã thêm cột StripeCustomerId vào bảng TaiKhoan thành công!';
END
ELSE
BEGIN
    PRINT 'Cột StripeCustomerId đã tồn tại trong bảng TaiKhoan.';
END
GO

-- Tạo index cho cột này để tìm kiếm nhanh hơn (optional nhưng recommended)
IF NOT EXISTS (
    SELECT * 
    FROM sys.indexes 
    WHERE name = 'IX_TaiKhoan_StripeCustomerId' 
    AND object_id = OBJECT_ID('TaiKhoan')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TaiKhoan_StripeCustomerId
    ON TaiKhoan (StripeCustomerId)
    WHERE StripeCustomerId IS NOT NULL;
    
    PRINT 'Đã tạo index IX_TaiKhoan_StripeCustomerId thành công!';
END
ELSE
BEGIN
    PRINT 'Index IX_TaiKhoan_StripeCustomerId đã tồn tại.';
END
GO

-- Kiểm tra kết quả
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TaiKhoan' 
AND COLUMN_NAME = 'StripeCustomerId';
GO
