-- =============================================
-- Script: Tạo các bảng cho hệ thống Khuyến Mãi
-- Date: 2025-12-06
-- Description: PHASE 1 - Foundation tables cho promotion system
-- =============================================

USE LittlefishBeauty;
GO

-- 1. Bảng KhuyenMai (Promotion)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KhuyenMai]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KhuyenMai](
        [ID_KhuyenMai] INT IDENTITY(1,1) NOT NULL,
        [TenKhuyenMai] NVARCHAR(200) NOT NULL,
        [MoTa] NVARCHAR(1000) NULL,
        [LoaiKhuyenMai] NVARCHAR(50) NOT NULL, -- GIAM_GIA_SAN_PHAM, GIAM_GIA_DON_HANG, MA_GIAM_GIA, FREESHIP, TANG_QUA, COMBO
        [HinhThucGiam] NVARCHAR(50) NOT NULL, -- PHAN_TRAM, SO_TIEN, GIA_CO_DINH
        [GiaTriGiam] DECIMAL(18, 2) NOT NULL,
        [GiaTriGiamToiDa] DECIMAL(18, 2) NULL, -- Giới hạn số tiền giảm tối đa khi giảm theo %
        [NgayBatDau] DATETIME NOT NULL,
        [NgayKetThuc] DATETIME NOT NULL,
        [GioBatDau] TIME NULL, -- Cho flash sale
        [GioKetThuc] TIME NULL, -- Cho flash sale
        [TrangThai] NVARCHAR(50) NOT NULL DEFAULT 'NHAP', -- NHAP, CHO_DUYET, DANG_HOAT_DONG, TAM_DUNG, KET_THUC
        [UuTien] INT NOT NULL DEFAULT 1, -- Ưu tiên khi stack nhiều KM
        [CoTheKetHop] BIT NOT NULL DEFAULT 0, -- Cho phép kết hợp với KM khác
        [HienThiTrangChu] BIT NOT NULL DEFAULT 0, -- Hiển thị banner trên trang chủ
        [AnhBanner] NVARCHAR(500) NULL, -- URL ảnh banner
        [SoLuongGioiHan] INT NULL, -- Giới hạn số lượng (flash sale)
        [SoLuongDaBan] INT NOT NULL DEFAULT 0, -- Tracking
        [NguoiTao] INT NULL,
        [NgayTao] DATETIME NULL DEFAULT GETDATE(),
        [NguoiSua] INT NULL,
        [NgaySua] DATETIME NULL,
        CONSTRAINT [PK__KhuyenMa__8C93C7F9A1B2C3D4] PRIMARY KEY CLUSTERED ([ID_KhuyenMai] ASC),
        CONSTRAINT [FK_KhuyenMai_NguoiTao] FOREIGN KEY([NguoiTao]) REFERENCES [dbo].[TaiKhoan] ([ID_TaiKhoan]),
        CONSTRAINT [FK_KhuyenMai_NguoiSua] FOREIGN KEY([NguoiSua]) REFERENCES [dbo].[TaiKhoan] ([ID_TaiKhoan])
    );
    
    PRINT 'Created table: KhuyenMai';
END
ELSE
BEGIN
    PRINT 'Table KhuyenMai already exists';
END
GO

-- 2. Bảng DieuKienKhuyenMai (Promotion Conditions)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DieuKienKhuyenMai]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DieuKienKhuyenMai](
        [ID_DieuKien] INT IDENTITY(1,1) NOT NULL,
        [ID_KhuyenMai] INT NOT NULL,
        [LoaiDieuKien] NVARCHAR(100) NOT NULL, -- DON_HANG_TOI_THIEU, SO_LUONG_SAN_PHAM, KHACH_HANG_MOI, etc.
        [GiaTri] NVARCHAR(200) NULL, -- Giá trị điều kiện
        [GhiChu] NVARCHAR(500) NULL,
        CONSTRAINT [PK__DieuKien__5E1F2A3B2C4D5E6F] PRIMARY KEY CLUSTERED ([ID_DieuKien] ASC),
        CONSTRAINT [FK_DieuKienKhuyenMai_KhuyenMai] FOREIGN KEY([ID_KhuyenMai]) 
            REFERENCES [dbo].[KhuyenMai] ([ID_KhuyenMai]) ON DELETE CASCADE
    );
    
    PRINT 'Created table: DieuKienKhuyenMai';
END
ELSE
BEGIN
    PRINT 'Table DieuKienKhuyenMai already exists';
END
GO

-- 3. Bảng KhuyenMaiSanPham (Promotion-Product Link)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KhuyenMaiSanPham]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KhuyenMaiSanPham](
        [ID_KhuyenMai] INT NOT NULL,
        [ID_SanPham] INT NOT NULL,
        [GiaKhuyenMai] DECIMAL(18, 2) NULL, -- Giá sau KM (tính sẵn)
        [SoLuongConLai] INT NULL, -- Cho flash sale
        CONSTRAINT [PK_KhuyenMaiSanPham] PRIMARY KEY CLUSTERED ([ID_KhuyenMai] ASC, [ID_SanPham] ASC),
        CONSTRAINT [FK_KhuyenMaiSanPham_KhuyenMai] FOREIGN KEY([ID_KhuyenMai]) 
            REFERENCES [dbo].[KhuyenMai] ([ID_KhuyenMai]) ON DELETE CASCADE,
        CONSTRAINT [FK_KhuyenMaiSanPham_SanPham] FOREIGN KEY([ID_SanPham]) 
            REFERENCES [dbo].[SanPham] ([ID_SanPham]) ON DELETE CASCADE
    );
    
    PRINT 'Created table: KhuyenMaiSanPham';
END
ELSE
BEGIN
    PRINT 'Table KhuyenMaiSanPham already exists';
END
GO

-- 4. Bảng KhuyenMaiDanhMuc (Promotion-Category Link)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KhuyenMaiDanhMuc]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[KhuyenMaiDanhMuc](
        [ID_KhuyenMai] INT NOT NULL,
        [ID_DanhMuc] INT NOT NULL,
        [ApDungChoSanPhamCon] BIT NOT NULL DEFAULT 1, -- Áp dụng cho danh mục con
        CONSTRAINT [PK_KhuyenMaiDanhMuc] PRIMARY KEY CLUSTERED ([ID_KhuyenMai] ASC, [ID_DanhMuc] ASC),
        CONSTRAINT [FK_KhuyenMaiDanhMuc_KhuyenMai] FOREIGN KEY([ID_KhuyenMai]) 
            REFERENCES [dbo].[KhuyenMai] ([ID_KhuyenMai]) ON DELETE CASCADE,
        CONSTRAINT [FK_KhuyenMaiDanhMuc_DanhMuc] FOREIGN KEY([ID_DanhMuc]) 
            REFERENCES [dbo].[DanhMuc] ([ID_DanhMuc]) ON DELETE CASCADE
    );
    
    PRINT 'Created table: KhuyenMaiDanhMuc';
END
ELSE
BEGIN
    PRINT 'Table KhuyenMaiDanhMuc already exists';
END
GO

-- Tạo indexes để tăng performance
CREATE NONCLUSTERED INDEX [IX_KhuyenMai_TrangThai] ON [dbo].[KhuyenMai] ([TrangThai]);
CREATE NONCLUSTERED INDEX [IX_KhuyenMai_NgayBatDau_NgayKetThuc] ON [dbo].[KhuyenMai] ([NgayBatDau], [NgayKetThuc]);
CREATE NONCLUSTERED INDEX [IX_KhuyenMaiSanPham_SanPham] ON [dbo].[KhuyenMaiSanPham] ([ID_SanPham]);
CREATE NONCLUSTERED INDEX [IX_KhuyenMaiDanhMuc_DanhMuc] ON [dbo].[KhuyenMaiDanhMuc] ([ID_DanhMuc]);
GO

PRINT 'Promotion tables created successfully!';
GO
