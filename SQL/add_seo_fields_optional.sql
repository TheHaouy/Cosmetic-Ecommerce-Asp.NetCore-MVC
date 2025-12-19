-- =============================================
-- OPTIONAL: Add SEO fields to SanPham table
-- Date: 2025-12-01
-- Description: Thêm các trường SEO tùy chỉnh cho sản phẩm
-- =============================================

USE LittleFishBeauty;
GO

PRINT '=== THÊM CÁC TRƯỜNG SEO (OPTIONAL) ===';
PRINT '';

-- Kiểm tra và thêm cột MetaTitle
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SanPham' 
    AND COLUMN_NAME = 'MetaTitle'
)
BEGIN
    ALTER TABLE SanPham
    ADD MetaTitle NVARCHAR(100) NULL;
    
    PRINT '✓ Đã thêm cột MetaTitle';
END
ELSE
BEGIN
    PRINT '→ Cột MetaTitle đã tồn tại';
END
GO

-- Kiểm tra và thêm cột MetaDescription
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SanPham' 
    AND COLUMN_NAME = 'MetaDescription'
)
BEGIN
    ALTER TABLE SanPham
    ADD MetaDescription NVARCHAR(200) NULL;
    
    PRINT '✓ Đã thêm cột MetaDescription';
END
ELSE
BEGIN
    PRINT '→ Cột MetaDescription đã tồn tại';
END
GO

-- Kiểm tra và thêm cột MetaKeywords
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SanPham' 
    AND COLUMN_NAME = 'MetaKeywords'
)
BEGIN
    ALTER TABLE SanPham
    ADD MetaKeywords NVARCHAR(200) NULL;
    
    PRINT '✓ Đã thêm cột MetaKeywords';
END
ELSE
BEGIN
    PRINT '→ Cột MetaKeywords đã tồn tại';
END
GO

-- Kiểm tra và thêm cột OgImage (Custom Open Graph image URL)
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SanPham' 
    AND COLUMN_NAME = 'OgImage'
)
BEGIN
    ALTER TABLE SanPham
    ADD OgImage NVARCHAR(500) NULL;
    
    PRINT '✓ Đã thêm cột OgImage';
END
ELSE
BEGIN
    PRINT '→ Cột OgImage đã tồn tại';
END
GO

PRINT '';
PRINT '=== GHI CHÚ ===';
PRINT 'Các trường này là TÙY CHỌN (optional).';
PRINT 'Nếu NULL, hệ thống sẽ tự động generate từ:';
PRINT '  - MetaTitle: Từ TenSanPham';
PRINT '  - MetaDescription: Từ MoTa (rút gọn)';
PRINT '  - MetaKeywords: Từ TenSanPham + TenDanhMuc';
PRINT '  - OgImage: Từ ảnh chính sản phẩm';
PRINT '';
PRINT 'Chỉ cần điền nếu muốn CUSTOM riêng cho từng sản phẩm!';
PRINT '';

-- Ví dụ cập nhật custom SEO cho một sản phẩm
/*
UPDATE SanPham
SET 
    MetaTitle = N'Kem Dưỡng Da Collagen - Trắng Da Hiệu Quả',
    MetaDescription = N'Kem dưỡng da chiết xuất collagen tự nhiên, giúp trắng da, mờ thâm, chống lão hóa. Cam kết chính hãng, giá tốt nhất.',
    MetaKeywords = N'kem dưỡng da, collagen, trắng da, chống lão hóa, mỹ phẩm'
WHERE ID_SanPham = 1;
*/

PRINT '=== HOÀN TẤT ===';
GO
