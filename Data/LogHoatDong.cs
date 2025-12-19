using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class LogHoatDong
{
    public int IdLog { get; set; }

    public int? IdTaiKhoan { get; set; }

    public string HanhDong { get; set; } = null!;

    public string? DoiTuong { get; set; }

    public int? IdDoiTuong { get; set; }

    public DateTime? ThoiGian { get; set; }

    public string? Ip { get; set; }

    public virtual TaiKhoan? IdTaiKhoanNavigation { get; set; }
}
