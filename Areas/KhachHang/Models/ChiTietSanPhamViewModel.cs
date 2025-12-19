using System;
using System.Collections.Generic;

namespace Final_VS1.Areas.KhachHang.Models
{
    public class ChiTietSanPhamViewModel
    {
        public int Id { get; set; }
        public int IdDanhMuc { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? Slug { get; set; } // THÊM DÒNG NÀY
        public string? MoTa { get; set; } = string.Empty;
        public decimal? GiaBan { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? TrangThai { get; set; }
        public string? CachSuDung { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? TenDanhMuc { get; set; }
        public string? DuongDanSEO { get; set; }
        public string? XuatXu { get; set; }
        public string? ThanhPhanChinh { get; set; }
        public List<AnhSanPhamViewModel> AnhSanPhams { get; set; } = new List<AnhSanPhamViewModel>();
        public List<BienTheSanPhamViewModel> BienTheSanPhams { get; set; } = new List<BienTheSanPhamViewModel>();
        public List<DanhGiaViewModel> DanhGias { get; set; } = new List<DanhGiaViewModel>();
    }

    public class AnhSanPhamViewModel
    {
        public int IdAnh { get; set; }
        public string? DuongDan { get; set; }
        public string? LoaiAnh { get; set; }
    }

    public class BienTheSanPhamViewModel
    {
        public int IdBienThe { get; set; }
        public string? Sku { get; set; }
        public int? SoLuongTonKho { get; set; }
        public decimal? GiaBan { get; set; }
        public List<ThuocTinhGiaTriViewModel> ThuocTinhGiaTris { get; set; } = new List<ThuocTinhGiaTriViewModel>();
    }

    public class ThuocTinhGiaTriViewModel
    {
        public int IdThuocTinh { get; set; }
        public string? TenThuocTinh { get; set; }
        public int IdGiaTri { get; set; }
        public string? GiaTri { get; set; }
    }

    public class DanhGiaViewModel
    {
        public int IdDanhGia { get; set; }
        public int IdTaiKhoan { get; set; }
        public int? SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public string? AnhDanhGia { get; set; }
        public DateTime? NgayDanhGia { get; set; }
        public string? HoTen { get; set; }
        public string? TraLoiCuaShop { get; set; }
        public DateTime? NgayTraLoi { get; set; }
    }
}