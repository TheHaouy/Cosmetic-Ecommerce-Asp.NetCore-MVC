-- Add LoaiDiaChi column to DiaChi table
-- This column stores address type: 'home', 'office', or 'other'

USE LittlefishBeauty;
GO

-- Check if column exists before adding
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[DiaChi]') 
    AND name = 'LoaiDiaChi'
)
BEGIN
    -- Add LoaiDiaChi column with default value
    ALTER TABLE [dbo].[DiaChi]
    ADD [LoaiDiaChi] NVARCHAR(50) NULL;
    
    PRINT 'Column LoaiDiaChi added successfully';
END
ELSE
BEGIN
    PRINT 'Column LoaiDiaChi already exists';
END
GO

-- Update existing records to have default value 'home'
UPDATE [dbo].[DiaChi]
SET [LoaiDiaChi] = 'home'
WHERE [LoaiDiaChi] IS NULL;
GO

PRINT 'Migration completed successfully';
GO
