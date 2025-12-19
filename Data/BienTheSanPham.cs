using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class BienTheSanPham
{
    public int IdBienThe { get; set; }

    public int? IdSanPham { get; set; }

    public string? Sku { get; set; }

    public int? SoLuongTonKho { get; set; }

    public decimal? GiaBan { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual SanPham? IdSanPhamNavigation { get; set; }

    public virtual ICollection<GiaTriThuocTinh> IdGiaTris { get; set; } = new List<GiaTriThuocTinh>();
}
