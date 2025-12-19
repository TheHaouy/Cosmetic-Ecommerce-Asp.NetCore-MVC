using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class ChatMessage
{
    public int IdChat { get; set; }

    public int IdTaiKhoanGui { get; set; }

    public int IdTaiKhoanNhan { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? ThoiGian { get; set; }

    public string? TrangThai { get; set; }

    public virtual TaiKhoan IdTaiKhoanGuiNavigation { get; set; } = null!;

    public virtual TaiKhoan IdTaiKhoanNhanNavigation { get; set; } = null!;
}
