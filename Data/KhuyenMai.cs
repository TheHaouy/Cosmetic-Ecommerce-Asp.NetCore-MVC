using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

/// <summary>
/// Bảng chính lưu thông tin chương trình khuyến mãi
/// </summary>
public partial class KhuyenMai
{
    public int IdKhuyenMai { get; set; }

    public string TenKhuyenMai { get; set; } = null!;

    public string? MoTa { get; set; }

    /// <summary>
    /// GIAM_GIA_SAN_PHAM, GIAM_GIA_DON_HANG, MA_GIAM_GIA, FREESHIP, TANG_QUA, COMBO
    /// </summary>
    public string LoaiKhuyenMai { get; set; } = null!;

    /// <summary>
    /// PHAN_TRAM, SO_TIEN, GIA_CO_DINH
    /// </summary>
    public string HinhThucGiam { get; set; } = null!;

    /// <summary>
    /// Giá trị giảm (% hoặc số tiền)
    /// </summary>
    public decimal GiaTriGiam { get; set; }

    /// <summary>
    /// Giới hạn số tiền giảm tối đa khi giảm theo %
    /// </summary>
    public decimal? GiaTriGiamToiDa { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    /// <summary>
    /// Giờ bắt đầu cho flash sale (nullable)
    /// </summary>
    public TimeSpan? GioBatDau { get; set; }

    /// <summary>
    /// Giờ kết thúc cho flash sale (nullable)
    /// </summary>
    public TimeSpan? GioKetThuc { get; set; }

    /// <summary>
    /// NHAP, CHO_DUYET, DANG_HOAT_DONG, TAM_DUNG, KET_THUC
    /// </summary>
    public string TrangThai { get; set; } = "NHAP";

    /// <summary>
    /// Ưu tiên khi có nhiều KM (số càng cao càng ưu tiên)
    /// </summary>
    public int UuTien { get; set; } = 1;

    /// <summary>
    /// Cho phép kết hợp với KM khác không
    /// </summary>
    public bool CoTheKetHop { get; set; } = false;

    /// <summary>
    /// Hiển thị banner trên trang chủ
    /// </summary>
    public bool HienThiTrangChu { get; set; } = false;

    /// <summary>
    /// URL ảnh banner
    /// </summary>
    public string? AnhBanner { get; set; }

    /// <summary>
    /// Giới hạn số lượng sản phẩm (flash sale)
    /// </summary>
    public int? SoLuongGioiHan { get; set; }

    /// <summary>
    /// Số lượng đã bán
    /// </summary>
    public int SoLuongDaBan { get; set; } = 0;

    /// <summary>
    /// Giá trị đơn hàng tối thiểu để áp dụng KM (cho loại GIAM_GIA_DON_HANG)
    /// </summary>
    public decimal? GiaTriDonHangToiThieu { get; set; }

    /// <summary>
    /// Giá trị giảm tối đa cho đơn hàng (cho loại GIAM_GIA_DON_HANG khi giảm theo %)
    /// </summary>
    public decimal? GiaTriGiamToiDaDonHang { get; set; }

    /// <summary>
    /// Ngày trong tuần áp dụng: "ALL" hoặc "2,3,4,5,6,7,CN" (Thứ 2 đến CN)
    /// </summary>
    public string? NgayApDung { get; set; } = "ALL";

    public int? NguoiTao { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? NguoiSua { get; set; }

    public DateTime? NgaySua { get; set; }

    // Navigation properties
    public virtual ICollection<DieuKienKhuyenMai> DieuKienKhuyenMais { get; set; } = new List<DieuKienKhuyenMai>();
    
    public virtual ICollection<KhuyenMaiSanPham> KhuyenMaiSanPhams { get; set; } = new List<KhuyenMaiSanPham>();
    
    public virtual ICollection<KhuyenMaiDanhMuc> KhuyenMaiDanhMucs { get; set; } = new List<KhuyenMaiDanhMuc>();

    public virtual TaiKhoan? NguoiTaoNavigation { get; set; }
    
    public virtual TaiKhoan? NguoiSuaNavigation { get; set; }
}
