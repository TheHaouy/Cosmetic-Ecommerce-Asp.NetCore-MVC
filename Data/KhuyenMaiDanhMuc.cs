using System;

namespace Final_VS1.Data;

/// <summary>
/// Áp dụng khuyến mãi cho cả danh mục sản phẩm
/// </summary>
public partial class KhuyenMaiDanhMuc
{
    public int IdKhuyenMai { get; set; }

    public int IdDanhMuc { get; set; }

    /// <summary>
    /// Có áp dụng cho danh mục con không
    /// </summary>
    public bool ApDungChoSanPhamCon { get; set; } = true;

    // Navigation properties
    public virtual KhuyenMai IdKhuyenMaiNavigation { get; set; } = null!;
    
    public virtual DanhMuc IdDanhMucNavigation { get; set; } = null!;
}
