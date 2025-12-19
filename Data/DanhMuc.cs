using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class DanhMuc
{
    public int IdDanhMuc { get; set; }

    public string? TenDanhMuc { get; set; }

    public string? MoTa { get; set; }

    public string? AnhDaiDien { get; set; }

    public int? IdDanhMucCha { get; set; }

    public string? DuongDanSeo { get; set; }

    public int? ThuTuHienThi { get; set; }

    public virtual DanhMuc? IdDanhMucChaNavigation { get; set; }

    public virtual ICollection<DanhMuc> InverseIdDanhMucChaNavigation { get; set; } = new List<DanhMuc>();

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
