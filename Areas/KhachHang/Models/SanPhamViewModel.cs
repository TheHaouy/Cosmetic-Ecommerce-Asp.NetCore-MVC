using System;
using System.Collections.Generic;

namespace Final_VS1.Areas.KhachHang.Models
{
    public class SanPhamViewModel
    {
    public int IdSanPham { get; set; }
    public string? TenSanPham { get; set; }
    public string? Slug { get; set; }
    public string? MoTa { get; set; }
    public decimal? GiaBan { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? TrangThai { get; set; }
    public int? IdDanhMuc { get; set; }
    public string? CachSuDung { get; set; }
    public DateTime? NgayTao { get; set; }
    public string? TenDanhMuc { get; set; }
    public List<string> AnhChinhs { get; set; } = new List<string>();
    public double DiemDanhGia { get; set; }
    public int SoLuongDanhGia { get; set; }
    public int? IdBienTheGiaThapNhat { get; set; }
    public bool HasVariants { get; set; }
    
    // Thông tin khuyến mãi
    public decimal? GiaKhuyenMai { get; set; }
    public decimal? PhanTramGiam { get; set; }
    public bool CoKhuyenMai => GiaKhuyenMai.HasValue && GiaKhuyenMai.Value < (GiaBan ?? 0);
    public string? TenKhuyenMai { get; set; }
    public bool IsFlashSale { get; set; }
    }
}
