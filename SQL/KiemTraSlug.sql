-- Script kiểm tra nhanh: Database đã có cột Slug chưa?
-- Chạy script này để kiểm tra trước khi chạy migration

PRINT '=== KIỂM TRA CỘT SLUG ==='
PRINT ''

-- Kiểm tra cột Slug trong bảng SanPham
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SanPham' AND COLUMN_NAME = 'Slug')
BEGIN
    PRINT '✅ Cột Slug ĐÃ TỒN TẠI trong bảng SanPham'
    PRINT ''
    
    -- Kiểm tra số lượng sản phẩm có slug
    DECLARE @TongSanPham INT
    DECLARE @SanPhamCoSlug INT
    DECLARE @SanPhamKhongCoSlug INT
    
    SELECT @TongSanPham = COUNT(*) FROM SanPham
    SELECT @SanPhamCoSlug = COUNT(*) FROM SanPham WHERE Slug IS NOT NULL AND Slug != ''
    SELECT @SanPhamKhongCoSlug = COUNT(*) FROM SanPham WHERE Slug IS NULL OR Slug = ''
    
    PRINT 'Thống kê:'
    PRINT '  - Tổng số sản phẩm: ' + CAST(@TongSanPham AS NVARCHAR(10))
    PRINT '  - Sản phẩm có slug: ' + CAST(@SanPhamCoSlug AS NVARCHAR(10))
    PRINT '  - Sản phẩm chưa có slug: ' + CAST(@SanPhamKhongCoSlug AS NVARCHAR(10))
    PRINT ''
    
    IF @SanPhamKhongCoSlug > 0
    BEGIN
        PRINT '⚠️  CÒN ' + CAST(@SanPhamKhongCoSlug AS NVARCHAR(10)) + ' SẢN PHẨM CHƯA CÓ SLUG!'
        PRINT '   → CẦN CHẠY MIGRATION: AddSlugToSanPham.sql'
    END
    ELSE
    BEGIN
        PRINT '✅ TẤT CẢ SẢN PHẨM ĐÃ CÓ SLUG!'
        PRINT '   → Không cần chạy migration nữa'
    END
    PRINT ''
    
    -- Hiển thị 5 sản phẩm mẫu
    PRINT 'Mẫu 5 sản phẩm đầu tiên:'
    SELECT TOP 5 
        ID_SanPham as [ID], 
        TenSanPham as [Tên Sản Phẩm],
        Slug as [Slug],
        CASE 
            WHEN Slug IS NULL OR Slug = '' THEN '❌ Chưa có'
            ELSE '✅ Có'
        END as [Trạng Thái]
    FROM SanPham
    ORDER BY ID_SanPham
END
ELSE
BEGIN
    PRINT '❌ CỘT SLUG CHƯA TỒN TẠI trong bảng SanPham'
    PRINT ''
    PRINT '📝 HƯỚNG DẪN:'
    PRINT '   1. Mở file: AddSlugToSanPham.sql'
    PRINT '   2. Nhấn F5 để chạy migration'
    PRINT '   3. Chạy lại script này để kiểm tra'
    PRINT ''
END

PRINT ''
PRINT '==================================='
