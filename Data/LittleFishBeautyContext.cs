using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Final_VS1.Data;

public partial class LittleFishBeautyContext : DbContext
{
    public LittleFishBeautyContext(DbContextOptions<LittleFishBeautyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AnhSanPham> AnhSanPhams { get; set; }

    public virtual DbSet<BienTheSanPham> BienTheSanPhams { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public virtual DbSet<DangNhapGoogle> DangNhapGoogles { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DiaChi> DiaChis { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GiaTriThuocTinh> GiaTriThuocTinhs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<DieuKienKhuyenMai> DieuKienKhuyenMais { get; set; }

    public virtual DbSet<KhuyenMaiSanPham> KhuyenMaiSanPhams { get; set; }

    public virtual DbSet<KhuyenMaiDanhMuc> KhuyenMaiDanhMucs { get; set; }

    public virtual DbSet<LogHoatDong> LogHoatDongs { get; set; }

    public virtual DbSet<MailMarketing> MailMarketings { get; set; }

    public virtual DbSet<PhanHoiDonHang> PhanHoiDonHangs { get; set; }

    public virtual DbSet<PhuongThucVanChuyen> PhuongThucVanChuyens { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SubscribeEmail> SubscribeEmails { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThanhPhan> ThanhPhans { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThuocTinh> ThuocTinhs { get; set; }

    public virtual DbSet<TimelineDonHang> TimelineDonHangs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnhSanPham>(entity =>
        {
            entity.HasKey(e => e.IdAnh).HasName("PK__AnhSanPh__2A42605DB6412A54");

            entity.ToTable("AnhSanPham");

            entity.Property(e => e.IdAnh).HasColumnName("ID_Anh");
            entity.Property(e => e.DuongDan).HasMaxLength(255);
            entity.Property(e => e.IdSanPham).HasColumnName("ID_SanPham");
            entity.Property(e => e.LinkCloudinary)
                .HasMaxLength(200)
                .HasColumnName("Link_cloudinary");
            entity.Property(e => e.LoaiAnh).HasMaxLength(20);

            entity.HasOne(d => d.IdSanPhamNavigation).WithMany(p => p.AnhSanPhams)
                .HasForeignKey(d => d.IdSanPham)
                .HasConstraintName("FK__AnhSanPha__ID_Sa__787EE5A0");
        });

        modelBuilder.Entity<BienTheSanPham>(entity =>
        {
            entity.HasKey(e => e.IdBienThe).HasName("PK__BienTheS__64C29E1002DB15B2");

            entity.ToTable("BienTheSanPham");

            entity.HasIndex(e => e.Sku, "UQ__BienTheS__CA1ECF0D4C7809EA").IsUnique();

            entity.Property(e => e.IdBienThe).HasColumnName("ID_BienThe");
            entity.Property(e => e.GiaBan).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.IdSanPham).HasColumnName("ID_SanPham");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");
            entity.Property(e => e.SoLuongTonKho).HasDefaultValue(0);

            entity.HasOne(d => d.IdSanPhamNavigation).WithMany(p => p.BienTheSanPhams)
                .HasForeignKey(d => d.IdSanPham)
                .HasConstraintName("FK__BienTheSa__ID_Sa__797309D9");

            entity.HasMany(d => d.IdGiaTris).WithMany(p => p.IdBienThes)
                .UsingEntity<Dictionary<string, object>>(
                    "ChiTietBienThe",
                    r => r.HasOne<GiaTriThuocTinh>().WithMany()
                        .HasForeignKey("IdGiaTri")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ChiTietBi__ID_Gi__7D439ABD"),
                    l => l.HasOne<BienTheSanPham>().WithMany()
                        .HasForeignKey("IdBienThe")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__ChiTietBi__ID_Bi__7C4F7684"),
                    j =>
                    {
                        j.HasKey("IdBienThe", "IdGiaTri").HasName("PK__ChiTietB__DB9B181E4D5CE997");
                        j.ToTable("ChiTietBienThe");
                        j.IndexerProperty<int>("IdBienThe").HasColumnName("ID_BienThe");
                        j.IndexerProperty<int>("IdGiaTri").HasColumnName("ID_GiaTri");
                    });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.IdChat).HasName("PK__ChatMess__73D612B41546D3C9");

            entity.ToTable("ChatMessage");

            entity.Property(e => e.IdChat).HasColumnName("ID_Chat");
            entity.Property(e => e.IdTaiKhoanGui).HasColumnName("ID_TaiKhoanGui");
            entity.Property(e => e.IdTaiKhoanNhan).HasColumnName("ID_TaiKhoanNhan");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdTaiKhoanGuiNavigation).WithMany(p => p.ChatMessageIdTaiKhoanGuiNavigations)
                .HasForeignKey(d => d.IdTaiKhoanGui)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessage_TaiKhoanGui");

            entity.HasOne(d => d.IdTaiKhoanNhanNavigation).WithMany(p => p.ChatMessageIdTaiKhoanNhanNavigations)
                .HasForeignKey(d => d.IdTaiKhoanNhan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatMessage_TaiKhoanNhan");
        });

        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.IdChiTiet).HasName("PK__ChiTietD__1EF2F705080D8094");

            entity.ToTable("ChiTietDonHang");

            entity.Property(e => e.IdChiTiet).HasColumnName("ID_ChiTiet");
            entity.Property(e => e.GiaLucDat).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.IdBienThe).HasColumnName("ID_BienThe");
            entity.Property(e => e.IdDonHang).HasColumnName("ID_DonHang");

            entity.HasOne(d => d.IdBienTheNavigation).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.IdBienThe)
                .HasConstraintName("FK__ChiTietDo__ID_Bi__7E37BEF6");

            entity.HasOne(d => d.IdDonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.IdDonHang)
                .HasConstraintName("FK__ChiTietDo__ID_Do__7F2BE32F");
        });

        modelBuilder.Entity<ChiTietGioHang>(entity =>
        {
            entity.HasKey(e => e.IdChiTiet).HasName("PK__ChiTietG__1EF2F7053769040F");

            entity.ToTable("ChiTietGioHang");

            entity.Property(e => e.IdChiTiet).HasColumnName("ID_ChiTiet");
            entity.Property(e => e.IdBienThe).HasColumnName("ID_BienThe");
            entity.Property(e => e.IdGioHang).HasColumnName("ID_GioHang");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.IdBienTheNavigation).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.IdBienThe)
                .HasConstraintName("FK__ChiTietGi__ID_Bi__00200768");

            entity.HasOne(d => d.IdGioHangNavigation).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.IdGioHang)
                .HasConstraintName("FK__ChiTietGi__ID_Gi__01142BA1");
        });

        modelBuilder.Entity<DangNhapGoogle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DangNhap__3214EC277D367A72");

            entity.ToTable("DangNhapGoogle");

            entity.HasIndex(e => e.GoogleId, "UQ_GoogleID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255)
                .HasColumnName("GoogleID");
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.DangNhapGoogles)
                .HasForeignKey(d => d.IdTaiKhoan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DangNhapGoogle_TaiKhoan");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.IdDanhGia).HasName("PK__DanhGia__6C898AE152B7D84F");

            entity.Property(e => e.IdDanhGia).HasColumnName("ID_DanhGia");
            entity.Property(e => e.AnhDanhGia).HasMaxLength(2000);
            entity.Property(e => e.BinhLuan).HasMaxLength(1000);
            entity.Property(e => e.IdSanPham).HasColumnName("ID_SanPham");
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TraLoiCuaShop).HasMaxLength(1000);
            entity.Property(e => e.NgayTraLoi).HasColumnType("datetime");

            entity.HasOne(d => d.IdSanPhamNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdSanPham)
                .HasConstraintName("FK__DanhGia__ID_SanP__02FC7413");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__DanhGia__ID_TaiK__03F0984C");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.IdDanhMuc).HasName("PK__DanhMuc__662ACB0181916410");

            entity.ToTable("DanhMuc");

            entity.Property(e => e.IdDanhMuc).HasColumnName("ID_DanhMuc");
            entity.Property(e => e.AnhDaiDien).HasMaxLength(255);
            entity.Property(e => e.DuongDanSeo)
                .HasMaxLength(255)
                .HasColumnName("DuongDanSEO");
            entity.Property(e => e.IdDanhMucCha).HasColumnName("ID_DanhMucCha");
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
            entity.Property(e => e.ThuTuHienThi).HasDefaultValue(0);

            entity.HasOne(d => d.IdDanhMucChaNavigation).WithMany(p => p.InverseIdDanhMucChaNavigation)
                .HasForeignKey(d => d.IdDanhMucCha)
                .HasConstraintName("FK__DanhMuc__ID_Danh__04E4BC85");
        });

        modelBuilder.Entity<DiaChi>(entity =>
        {
            entity.HasKey(e => e.IdDiaChi).HasName("PK__DiaChi__C793F2526A4BF583");

            entity.ToTable("DiaChi");

            entity.Property(e => e.IdDiaChi).HasColumnName("ID_DiaChi");
            entity.Property(e => e.DiaChiChiTiet).HasMaxLength(255);
            entity.Property(e => e.HoTenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.LoaiDiaChi).HasMaxLength(50);
            entity.Property(e => e.MacDinh).HasDefaultValue(false);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.DiaChis)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__DiaChi__ID_TaiKh__05D8E0BE");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.IdDonHang).HasName("PK__DonHang__99B726393BBCC202");

            entity.ToTable("DonHang");

            entity.Property(e => e.IdDonHang).HasColumnName("ID_DonHang");
            entity.Property(e => e.IdDiaChi).HasColumnName("ID_DiaChi");
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.IdVc).HasColumnName("ID_VC");
            entity.Property(e => e.NgayDat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(50);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdDiaChiNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdDiaChi)
                .HasConstraintName("FK__DonHang__ID_DiaC__06CD04F7");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__DonHang__ID_TaiK__07C12930");

            entity.HasOne(d => d.IdVcNavigation).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.IdVc)
                .HasConstraintName("FK_DonHang_PhuongThucVanChuyen");
        });

        modelBuilder.Entity<GiaTriThuocTinh>(entity =>
        {
            entity.HasKey(e => e.IdGiaTri).HasName("PK__GiaTriTh__F59860EAF6E4A059");

            entity.ToTable("GiaTriThuocTinh");

            entity.Property(e => e.IdGiaTri).HasColumnName("ID_GiaTri");
            entity.Property(e => e.GiaTri).HasMaxLength(100);
            entity.Property(e => e.IdThuocTinh).HasColumnName("ID_ThuocTinh");

            entity.HasOne(d => d.IdThuocTinhNavigation).WithMany(p => p.GiaTriThuocTinhs)
                .HasForeignKey(d => d.IdThuocTinh)
                .HasConstraintName("FK__GiaTriThu__ID_Th__09A971A2");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.IdGioHang).HasName("PK__GioHang__C033AA17120F6D1B");

            entity.ToTable("GioHang");

            entity.Property(e => e.IdGioHang).HasColumnName("ID_GioHang");
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__GioHang__ID_TaiK__0A9D95DB");
        });

        modelBuilder.Entity<LogHoatDong>(entity =>
        {
            entity.HasKey(e => e.IdLog).HasName("PK__LogHoatD__2DBF3395A4D898FA");

            entity.ToTable("LogHoatDong");

            entity.Property(e => e.IdLog).HasColumnName("ID_Log");
            entity.Property(e => e.DoiTuong).HasMaxLength(100);
            entity.Property(e => e.HanhDong).HasMaxLength(255);
            entity.Property(e => e.IdDoiTuong).HasColumnName("ID_DoiTuong");
            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.LogHoatDongs)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK_LogHoatDong_TaiKhoan");
        });

        modelBuilder.Entity<MailMarketing>(entity =>
        {
            entity.HasKey(e => e.IdMail).HasName("PK__MailMark__7E200889DECDC867");

            entity.ToTable("MailMarketing");

            entity.Property(e => e.IdMail).HasColumnName("ID_Mail");
            entity.Property(e => e.NgayGui)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NhomKhach).HasMaxLength(100);
        });

        modelBuilder.Entity<PhanHoiDonHang>(entity =>
        {
            entity.HasKey(e => e.IdPhanHoi).HasName("PK__PhanHoiD__8FC78EF09232ACE9");

            entity.ToTable("PhanHoiDonHang");

            entity.Property(e => e.IdPhanHoi).HasColumnName("ID_PhanHoi");
            entity.Property(e => e.IdDonHang).HasColumnName("ID_DonHang");
            entity.Property(e => e.NgayPhanHoi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdDonHangNavigation).WithMany(p => p.PhanHoiDonHangs)
                .HasForeignKey(d => d.IdDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanHoiDonHang_DonHang");
        });

        modelBuilder.Entity<PhuongThucVanChuyen>(entity =>
        {
            entity.HasKey(e => e.IdVc).HasName("PK__PhuongTh__8B63A17E5AEF7C9E");

            entity.ToTable("PhuongThucVanChuyen");

            entity.Property(e => e.IdVc).HasColumnName("ID_VC");
            entity.Property(e => e.Apikey)
                .HasMaxLength(255)
                .HasColumnName("APIKey");
            entity.Property(e => e.PhiVc)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("PhiVC");
            entity.Property(e => e.TenVc)
                .HasMaxLength(100)
                .HasColumnName("TenVC");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.IdSanPham).HasName("PK__SanPham__617EA392FAC14B56");

            entity.ToTable("SanPham");

            entity.HasIndex(e => e.Slug, "IX_SanPham_Slug").IsUnique();

            entity.Property(e => e.IdSanPham).HasColumnName("ID_SanPham");
            entity.Property(e => e.IdDanhMuc).HasColumnName("ID_DanhMuc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .IsRequired(false);
            entity.Property(e => e.TenSanPham).HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdDanhMucNavigation).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.IdDanhMuc)
                .HasConstraintName("FK__SanPham__ID_Danh__0D7A0286");

            entity.HasMany(d => d.IdThanhPhans).WithMany(p => p.IdSanPhams)
                .UsingEntity<Dictionary<string, object>>(
                    "SanPhamThanhPhan",
                    r => r.HasOne<ThanhPhan>().WithMany()
                        .HasForeignKey("IdThanhPhan")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__SanPham_T__ID_Th__0F624AF8"),
                    l => l.HasOne<SanPham>().WithMany()
                        .HasForeignKey("IdSanPham")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__SanPham_T__ID_Sa__0E6E26BF"),
                    j =>
                    {
                        j.HasKey("IdSanPham", "IdThanhPhan").HasName("PK__SanPham___52984147E7616660");
                        j.ToTable("SanPham_ThanhPhan");
                        j.IndexerProperty<int>("IdSanPham").HasColumnName("ID_SanPham");
                        j.IndexerProperty<int>("IdThanhPhan").HasColumnName("ID_ThanhPhan");
                    });
        });

        modelBuilder.Entity<SubscribeEmail>(entity =>
        {
            entity.HasKey(e => e.IdSub).HasName("PK__Subscrib__27F89E889373045E");

            entity.ToTable("SubscribeEmail");

            entity.HasIndex(e => e.Email, "UQ_SubscribeEmail").IsUnique();

            entity.Property(e => e.IdSub).HasColumnName("ID_Sub");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.NgayDangKy)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.IdTaiKhoan).HasName("PK__TaiKhoan__0E3EC21049DC8528");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.Email, "UQ__TaiKhoan__A9D1053483FC35CB").IsUnique();

            entity.Property(e => e.IdTaiKhoan).HasColumnName("ID_TaiKhoan");
            entity.Property(e => e.AnhDaiDien).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
            entity.Property(e => e.VaiTro).HasMaxLength(20);
        });

        modelBuilder.Entity<ThanhPhan>(entity =>
        {
            entity.HasKey(e => e.IdThanhPhan).HasName("PK__ThanhPha__3E6E2D52F90FD5EF");

            entity.ToTable("ThanhPhan");

            entity.Property(e => e.IdThanhPhan).HasColumnName("ID_ThanhPhan");
            entity.Property(e => e.TenThanhPhan).HasMaxLength(255);
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.IdThanhToan).HasName("PK__ThanhToa__AB2E5631DD215677");

            entity.ToTable("ThanhToan");

            entity.HasIndex(e => e.IdDonHang, "IX_ThanhToan_ID_DonHang");

            entity.HasIndex(e => e.MaGiaoDichNganHang, "IX_ThanhToan_MaGiaoDichNganHang");

            entity.Property(e => e.IdThanhToan).HasColumnName("ID_ThanhToan");
            entity.Property(e => e.IdDonHang).HasColumnName("ID_DonHang");
            entity.Property(e => e.MaGiaoDichNganHang)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaNganHang)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MaPhanHoi)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDungThanhToan).HasMaxLength(255);
            entity.Property(e => e.PhuongThuc).HasMaxLength(50);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThoiGianThanhToan).HasColumnType("datetime");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdDonHangNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.IdDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThanhToan_DonHang");
        });

        modelBuilder.Entity<ThuocTinh>(entity =>
        {
            entity.HasKey(e => e.IdThuocTinh).HasName("PK__ThuocTin__C49F0885BDC97067");

            entity.ToTable("ThuocTinh");

            entity.Property(e => e.IdThuocTinh).HasColumnName("ID_ThuocTinh");
            entity.Property(e => e.TenThuocTinh).HasMaxLength(100);
        });

        modelBuilder.Entity<TimelineDonHang>(entity =>
        {
            entity.HasKey(e => e.IdTimeline).HasName("PK__Timeline__5CF3344F4121B211");

            entity.ToTable("TimelineDonHang");

            entity.Property(e => e.IdTimeline).HasColumnName("ID_Timeline");
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.IdDonHang).HasColumnName("ID_DonHang");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThaiMoi).HasMaxLength(50);

            entity.HasOne(d => d.IdDonHangNavigation).WithMany(p => p.TimelineDonHangs)
                .HasForeignKey(d => d.IdDonHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TimelineDonHang_DonHang");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.IdKhuyenMai).HasName("PK__KhuyenMa__8C93C7F9A1B2C3D4");

            entity.ToTable("KhuyenMai");

            entity.Property(e => e.IdKhuyenMai).HasColumnName("ID_KhuyenMai");
            entity.Property(e => e.TenKhuyenMai)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(e => e.MoTa).HasMaxLength(1000);
            entity.Property(e => e.LoaiKhuyenMai)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.HinhThucGiam)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.GiaTriGiam).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaTriGiamToiDa).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("NHAP");
            entity.Property(e => e.UuTien).HasDefaultValue(1);
            entity.Property(e => e.CoTheKetHop).HasDefaultValue(false);
            entity.Property(e => e.HienThiTrangChu).HasDefaultValue(false);
            entity.Property(e => e.AnhBanner).HasMaxLength(500);
            entity.Property(e => e.SoLuongDaBan).HasDefaultValue(0);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NgaySua).HasColumnType("datetime");

            entity.HasOne(d => d.NguoiTaoNavigation).WithMany()
                .HasForeignKey(d => d.NguoiTao)
                .HasConstraintName("FK_KhuyenMai_NguoiTao");

            entity.HasOne(d => d.NguoiSuaNavigation).WithMany()
                .HasForeignKey(d => d.NguoiSua)
                .HasConstraintName("FK_KhuyenMai_NguoiSua");
        });

        modelBuilder.Entity<DieuKienKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.IdDieuKien).HasName("PK__DieuKien__5E1F2A3B2C4D5E6F");

            entity.ToTable("DieuKienKhuyenMai");

            entity.Property(e => e.IdDieuKien).HasColumnName("ID_DieuKien");
            entity.Property(e => e.IdKhuyenMai).HasColumnName("ID_KhuyenMai");
            entity.Property(e => e.LoaiDieuKien)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.GiaTri).HasMaxLength(200);
            entity.Property(e => e.GhiChu).HasMaxLength(500);

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.DieuKienKhuyenMais)
                .HasForeignKey(d => d.IdKhuyenMai)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DieuKienKhuyenMai_KhuyenMai");
        });

        modelBuilder.Entity<KhuyenMaiSanPham>(entity =>
        {
            entity.HasKey(e => new { e.IdKhuyenMai, e.IdSanPham })
                .HasName("PK_KhuyenMaiSanPham");

            entity.ToTable("KhuyenMaiSanPham");

            entity.Property(e => e.IdKhuyenMai).HasColumnName("ID_KhuyenMai");
            entity.Property(e => e.IdSanPham).HasColumnName("ID_SanPham");
            entity.Property(e => e.GiaKhuyenMai).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.KhuyenMaiSanPhams)
                .HasForeignKey(d => d.IdKhuyenMai)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_KhuyenMaiSanPham_KhuyenMai");

            entity.HasOne(d => d.IdSanPhamNavigation).WithMany()
                .HasForeignKey(d => d.IdSanPham)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_KhuyenMaiSanPham_SanPham");
        });

        modelBuilder.Entity<KhuyenMaiDanhMuc>(entity =>
        {
            entity.HasKey(e => new { e.IdKhuyenMai, e.IdDanhMuc })
                .HasName("PK_KhuyenMaiDanhMuc");

            entity.ToTable("KhuyenMaiDanhMuc");

            entity.Property(e => e.IdKhuyenMai).HasColumnName("ID_KhuyenMai");
            entity.Property(e => e.IdDanhMuc).HasColumnName("ID_DanhMuc");
            entity.Property(e => e.ApDungChoSanPhamCon).HasDefaultValue(true);

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.KhuyenMaiDanhMucs)
                .HasForeignKey(d => d.IdKhuyenMai)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_KhuyenMaiDanhMuc_KhuyenMai");

            entity.HasOne(d => d.IdDanhMucNavigation).WithMany()
                .HasForeignKey(d => d.IdDanhMuc)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_KhuyenMaiDanhMuc_DanhMuc");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
