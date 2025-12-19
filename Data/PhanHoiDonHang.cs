using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class PhanHoiDonHang
{
    public int IdPhanHoi { get; set; }

    public int IdDonHang { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayPhanHoi { get; set; }

    public virtual DonHang IdDonHangNavigation { get; set; } = null!;
}
