using System;

namespace Final_VS1.Data;

/// <summary>
/// Liên kết khuyến mãi với sản phẩm cụ thể
/// </summary>
public partial class KhuyenMaiSanPham
{
    public int IdKhuyenMai { get; set; }

    public int IdSanPham { get; set; }

    /// <summary>
    /// Giá sau khi áp dụng khuyến mãi (tính sẵn)
    /// </summary>
    public decimal? GiaKhuyenMai { get; set; }

    /// <summary>
    /// Số lượng còn lại cho flash sale
    /// </summary>
    public int? SoLuongConLai { get; set; }

    // Navigation properties
    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;
    
    public virtual SanPham IdSanPhamNavigation { get; set; } = null!;
}
