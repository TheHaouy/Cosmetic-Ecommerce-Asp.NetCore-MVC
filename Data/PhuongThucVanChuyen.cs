using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class PhuongThucVanChuyen
{
    public int IdVc { get; set; }

    public string TenVc { get; set; } = null!;

    public decimal? PhiVc { get; set; }

    public string? Apikey { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
