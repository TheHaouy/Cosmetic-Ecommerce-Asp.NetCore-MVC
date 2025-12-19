-- Migration: Add NhanEmailMarketing column to TaiKhoan table
-- Date: 2025-11-29
-- Description: Thêm trường NhanEmailMarketing để theo dõi người dùng đăng ký nhận email marketing

USE LittleFishBeauty;
GO

-- Kiểm tra nếu cột chưa tồn tại thì mới thêm
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TaiKhoan' 
    AND COLUMN_NAME = 'NhanEmailMarketing'
)
BEGIN
    ALTER TABLE TaiKhoan
    ADD NhanEmailMarketing BIT NULL DEFAULT 0;
    
    PRINT 'Đã thêm cột NhanEmailMarketing vào bảng TaiKhoan';
END
ELSE
BEGIN
    PRINT 'Cột NhanEmailMarketing đã tồn tại trong bảng TaiKhoan';
END
GO

-- Cập nhật giá trị mặc định cho các record hiện có (nếu cần)
UPDATE TaiKhoan
SET NhanEmailMarketing = 0
WHERE NhanEmailMarketing IS NULL;
GO

PRINT 'Migration hoàn tất!';
GO
