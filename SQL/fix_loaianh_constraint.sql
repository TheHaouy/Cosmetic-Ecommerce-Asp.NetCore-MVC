-- Script để xóa CHECK constraint trên cột LoaiAnh của bảng AnhSanPham
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- Xóa constraint cũ
ALTER TABLE AnhSanPham DROP CONSTRAINT CK__AnhSanPha__LoaiA__160F4887;

-- Nếu tên constraint khác, có thể dùng script sau để tìm tên chính xác:
-- SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('AnhSanPham');

-- Sau khi xóa constraint, bạn có thể thêm constraint mới nếu cần:
-- ALTER TABLE AnhSanPham ADD CONSTRAINT CK_AnhSanPham_LoaiAnh 
--     CHECK (LoaiAnh IN (N'Chính', N'Phụ', N'chinh', N'phu', N'Primary', N'Main', N'Secondary'));
