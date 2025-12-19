using System;
using System.Collections.Generic;

namespace Final_VS1.Data;
public partial class ThanhToan
{
    public int IdThanhToan { get; set; }

    public int IdDonHang { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string? MaGiaoDichNganHang { get; set; }

    public string? MaNganHang { get; set; }

    public DateTime? ThoiGianThanhToan { get; set; }

    public string? MaPhanHoi { get; set; }

    public string? NoiDungThanhToan { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual DonHang IdDonHangNavigation { get; set; } = null!;
}
