using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class TimelineDonHang
{
    public int IdTimeline { get; set; }

    public int IdDonHang { get; set; }

    public string TrangThaiMoi { get; set; } = null!;

    public string? GhiChu { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual DonHang IdDonHangNavigation { get; set; } = null!;
}
