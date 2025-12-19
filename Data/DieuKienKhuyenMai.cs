using System;

namespace Final_VS1.Data;

/// <summary>
/// Điều kiện để được áp dụng khuyến mãi
/// </summary>
public partial class DieuKienKhuyenMai
{
    public int IdDieuKien { get; set; }

    public int IdKhuyenMai { get; set; }

    /// <summary>
    /// DON_HANG_TOI_THIEU, SO_LUONG_SAN_PHAM, KHACH_HANG_MOI, KHACH_HANG_THANH_VIEN,
    /// NHOM_KHACH_HANG, TONG_TICH_LUY, NGAY_SINH_NHAT, DANH_MUC_SAN_PHAM, VUNG_MIEN
    /// </summary>
    public string LoaiDieuKien { get; set; } = null!;

    /// <summary>
    /// Giá trị điều kiện (số tiền, số lượng, ID nhóm khách hàng...)
    /// </summary>
    public string? GiaTri { get; set; }

    public string? GhiChu { get; set; }

    // Navigation property
    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;
}
