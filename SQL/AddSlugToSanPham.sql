-- Migration: Add Slug column to SanPham table
-- Date: 2025-11-29
-- Description: Add SEO-friendly slug column for products

-- Step 1: Add Slug column to SanPham table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SanPham' AND COLUMN_NAME = 'Slug')
BEGIN
    ALTER TABLE SanPham
    ADD Slug NVARCHAR(200) NULL;
    
    PRINT 'Slug column added successfully';
END
ELSE
BEGIN
    PRINT 'Slug column already exists';
END
GO

-- Step 2: Create unique index on Slug column (for performance and uniqueness)
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_SanPham_Slug' AND object_id = OBJECT_ID('SanPham'))
BEGIN
    CREATE UNIQUE INDEX IX_SanPham_Slug ON SanPham(Slug)
    WHERE Slug IS NOT NULL;
    
    PRINT 'Unique index on Slug created successfully';
END
ELSE
BEGIN
    PRINT 'Unique index on Slug already exists';
END
GO

-- Step 3: Generate slugs for existing products
-- This function removes Vietnamese diacritics and creates URL-friendly slugs
DECLARE @IdSanPham INT;
DECLARE @TenSanPham NVARCHAR(100);
DECLARE @Slug NVARCHAR(200);
DECLARE @Counter INT;
DECLARE @TempSlug NVARCHAR(200);

DECLARE product_cursor CURSOR FOR
SELECT ID_SanPham, TenSanPham
FROM SanPham
WHERE Slug IS NULL OR Slug = '';

OPEN product_cursor;

FETCH NEXT FROM product_cursor INTO @IdSanPham, @TenSanPham;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Generate base slug: lowercase, remove special chars, replace spaces with hyphens
    SET @Slug = LOWER(LTRIM(RTRIM(@TenSanPham)));
    
    -- Remove Vietnamese diacritics (basic replacement)
    SET @Slug = REPLACE(@Slug, N'á', 'a');
    SET @Slug = REPLACE(@Slug, N'à', 'a');
    SET @Slug = REPLACE(@Slug, N'ả', 'a');
    SET @Slug = REPLACE(@Slug, N'ã', 'a');
    SET @Slug = REPLACE(@Slug, N'ạ', 'a');
    SET @Slug = REPLACE(@Slug, N'ă', 'a');
    SET @Slug = REPLACE(@Slug, N'ắ', 'a');
    SET @Slug = REPLACE(@Slug, N'ằ', 'a');
    SET @Slug = REPLACE(@Slug, N'ẳ', 'a');
    SET @Slug = REPLACE(@Slug, N'ẵ', 'a');
    SET @Slug = REPLACE(@Slug, N'ặ', 'a');
    SET @Slug = REPLACE(@Slug, N'â', 'a');
    SET @Slug = REPLACE(@Slug, N'ấ', 'a');
    SET @Slug = REPLACE(@Slug, N'ầ', 'a');
    SET @Slug = REPLACE(@Slug, N'ẩ', 'a');
    SET @Slug = REPLACE(@Slug, N'ẫ', 'a');
    SET @Slug = REPLACE(@Slug, N'ậ', 'a');
    
    SET @Slug = REPLACE(@Slug, N'é', 'e');
    SET @Slug = REPLACE(@Slug, N'è', 'e');
    SET @Slug = REPLACE(@Slug, N'ẻ', 'e');
    SET @Slug = REPLACE(@Slug, N'ẽ', 'e');
    SET @Slug = REPLACE(@Slug, N'ẹ', 'e');
    SET @Slug = REPLACE(@Slug, N'ê', 'e');
    SET @Slug = REPLACE(@Slug, N'ế', 'e');
    SET @Slug = REPLACE(@Slug, N'ề', 'e');
    SET @Slug = REPLACE(@Slug, N'ể', 'e');
    SET @Slug = REPLACE(@Slug, N'ễ', 'e');
    SET @Slug = REPLACE(@Slug, N'ệ', 'e');
    
    SET @Slug = REPLACE(@Slug, N'í', 'i');
    SET @Slug = REPLACE(@Slug, N'ì', 'i');
    SET @Slug = REPLACE(@Slug, N'ỉ', 'i');
    SET @Slug = REPLACE(@Slug, N'ĩ', 'i');
    SET @Slug = REPLACE(@Slug, N'ị', 'i');
    
    SET @Slug = REPLACE(@Slug, N'ó', 'o');
    SET @Slug = REPLACE(@Slug, N'ò', 'o');
    SET @Slug = REPLACE(@Slug, N'ỏ', 'o');
    SET @Slug = REPLACE(@Slug, N'õ', 'o');
    SET @Slug = REPLACE(@Slug, N'ọ', 'o');
    SET @Slug = REPLACE(@Slug, N'ô', 'o');
    SET @Slug = REPLACE(@Slug, N'ố', 'o');
    SET @Slug = REPLACE(@Slug, N'ồ', 'o');
    SET @Slug = REPLACE(@Slug, N'ổ', 'o');
    SET @Slug = REPLACE(@Slug, N'ỗ', 'o');
    SET @Slug = REPLACE(@Slug, N'ộ', 'o');
    SET @Slug = REPLACE(@Slug, N'ơ', 'o');
    SET @Slug = REPLACE(@Slug, N'ớ', 'o');
    SET @Slug = REPLACE(@Slug, N'ờ', 'o');
    SET @Slug = REPLACE(@Slug, N'ở', 'o');
    SET @Slug = REPLACE(@Slug, N'ỡ', 'o');
    SET @Slug = REPLACE(@Slug, N'ợ', 'o');
    
    SET @Slug = REPLACE(@Slug, N'ú', 'u');
    SET @Slug = REPLACE(@Slug, N'ù', 'u');
    SET @Slug = REPLACE(@Slug, N'ủ', 'u');
    SET @Slug = REPLACE(@Slug, N'ũ', 'u');
    SET @Slug = REPLACE(@Slug, N'ụ', 'u');
    SET @Slug = REPLACE(@Slug, N'ư', 'u');
    SET @Slug = REPLACE(@Slug, N'ứ', 'u');
    SET @Slug = REPLACE(@Slug, N'ừ', 'u');
    SET @Slug = REPLACE(@Slug, N'ử', 'u');
    SET @Slug = REPLACE(@Slug, N'ữ', 'u');
    SET @Slug = REPLACE(@Slug, N'ự', 'u');
    
    SET @Slug = REPLACE(@Slug, N'ý', 'y');
    SET @Slug = REPLACE(@Slug, N'ỳ', 'y');
    SET @Slug = REPLACE(@Slug, N'ỷ', 'y');
    SET @Slug = REPLACE(@Slug, N'ỹ', 'y');
    SET @Slug = REPLACE(@Slug, N'ỵ', 'y');
    
    SET @Slug = REPLACE(@Slug, N'đ', 'd');
    
    -- Remove special characters, keep only letters, numbers, and spaces
    SET @Slug = REPLACE(@Slug, ',', '');
    SET @Slug = REPLACE(@Slug, '.', '');
    SET @Slug = REPLACE(@Slug, '!', '');
    SET @Slug = REPLACE(@Slug, '?', '');
    SET @Slug = REPLACE(@Slug, '(', '');
    SET @Slug = REPLACE(@Slug, ')', '');
    SET @Slug = REPLACE(@Slug, '[', '');
    SET @Slug = REPLACE(@Slug, ']', '');
    SET @Slug = REPLACE(@Slug, '{', '');
    SET @Slug = REPLACE(@Slug, '}', '');
    SET @Slug = REPLACE(@Slug, '/', '');
    SET @Slug = REPLACE(@Slug, '\', '');
    SET @Slug = REPLACE(@Slug, '|', '');
    SET @Slug = REPLACE(@Slug, '<', '');
    SET @Slug = REPLACE(@Slug, '>', '');
    SET @Slug = REPLACE(@Slug, ':', '');
    SET @Slug = REPLACE(@Slug, ';', '');
    SET @Slug = REPLACE(@Slug, '"', '');
    SET @Slug = REPLACE(@Slug, '''', '');
    SET @Slug = REPLACE(@Slug, '`', '');
    SET @Slug = REPLACE(@Slug, '~', '');
    SET @Slug = REPLACE(@Slug, '@', '');
    SET @Slug = REPLACE(@Slug, '#', '');
    SET @Slug = REPLACE(@Slug, '$', '');
    SET @Slug = REPLACE(@Slug, '%', '');
    SET @Slug = REPLACE(@Slug, '^', '');
    SET @Slug = REPLACE(@Slug, '&', '');
    SET @Slug = REPLACE(@Slug, '*', '');
    SET @Slug = REPLACE(@Slug, '+', '');
    SET @Slug = REPLACE(@Slug, '=', '');
    
    -- Replace spaces with hyphens
    WHILE CHARINDEX('  ', @Slug) > 0
        SET @Slug = REPLACE(@Slug, '  ', ' ');
    
    SET @Slug = REPLACE(@Slug, ' ', '-');
    
    -- Remove multiple consecutive hyphens
    WHILE CHARINDEX('--', @Slug) > 0
        SET @Slug = REPLACE(@Slug, '--', '-');
    
    -- Trim hyphens from start and end
    WHILE LEFT(@Slug, 1) = '-'
        SET @Slug = SUBSTRING(@Slug, 2, LEN(@Slug) - 1);
    
    WHILE RIGHT(@Slug, 1) = '-'
        SET @Slug = SUBSTRING(@Slug, 1, LEN(@Slug) - 1);
    
    -- Check if slug already exists, if yes add counter
    SET @Counter = 1;
    SET @TempSlug = @Slug;
    
    WHILE EXISTS (SELECT 1 FROM SanPham WHERE Slug = @TempSlug AND ID_SanPham != @IdSanPham)
    BEGIN
        SET @TempSlug = @Slug + '-' + CAST(@Counter AS NVARCHAR(10));
        SET @Counter = @Counter + 1;
    END
    
    -- Update the product with the generated slug
    UPDATE SanPham
    SET Slug = @TempSlug
    WHERE ID_SanPham = @IdSanPham;
    
    PRINT 'Generated slug for product ID ' + CAST(@IdSanPham AS NVARCHAR(10)) + ': ' + @TempSlug;
    
    FETCH NEXT FROM product_cursor INTO @IdSanPham, @TenSanPham;
END

CLOSE product_cursor;
DEALLOCATE product_cursor;

PRINT 'Migration completed successfully!';
GO

-- Step 4: Verify results
SELECT ID_SanPham, TenSanPham, Slug
FROM SanPham
ORDER BY ID_SanPham;
GO
