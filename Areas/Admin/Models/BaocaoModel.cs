namespace Final_VS1.Areas.Admin.Models
{
    public class BaocaoModel
    {
        // Thống kê tổng quan
        public int TongSoSanPham { get; set; }
        public int TongSoDonHang { get; set; }
        public int TongSoDanhMuc { get; set; }

        // Đơn hàng theo trạng thái
        public int DonHangChoXuLy { get; set; }
        public int DonHangDangGiao { get; set; }
        public int DonHangDaGiao { get; set; }
        public int DonHangDaHuy { get; set; }

        // Thống kê khách hàng
        public int TongSoKhachHang { get; set; }
        public int KhachHangMoiThangNay { get; set; }
        public int KhachHangQuayLai { get; set; }
        public double TyLeChuyenDoi { get; set; }
        public double DanhGiaTrungBinh { get; set; }
        public double DonHangTrungBinhMoiKhach { get; set; }
        public decimal GiaTriTrungBinhMoiKhach { get; set; }

        // Thống kê đánh giá
        public int TongSoDanhGia { get; set; }
        public int SanPhamCoDanhGia { get; set; }
        public List<DanhGiaTheoSaoModel> DanhGiaTheoSao { get; set; } = new List<DanhGiaTheoSaoModel>();
        public List<SanPhamDanhGiaModel> TopSanPhamDanhGia { get; set; } = new List<SanPhamDanhGiaModel>();

        // Phân khúc khách hàng
        public int KhachHangVIP { get; set; }
        public int KhachHangThuong { get; set; }
        public int KhachHangMoi { get; set; }

        // Hoạt động theo tuần
        public List<HoatDongTuanModel> HoatDongTuan { get; set; } = new List<HoatDongTuanModel>();

        // Top sản phẩm bán chạy
        public List<TopSanPhamModel> TopSanPhamBanChay { get; set; } = new List<TopSanPhamModel>();

        // Thống kê theo danh mục
        public List<ThongKeDanhMucModel> ThongKeDanhMuc { get; set; } = new List<ThongKeDanhMucModel>();

        // Doanh thu 12 tháng
        public List<DoanhThuThangModel> DoanhThu12Thang { get; set; } = new List<DoanhThuThangModel>();
    // Tổng doanh thu
    public decimal TongDoanhThu { get; set; }
    }

    public class HoatDongTuanModel
    {
        public string NgayTrongTuan { get; set; } = string.Empty;
        public int SoLuongTruyCap { get; set; }
        public int SoDonHang { get; set; }
    }

    public class TopSanPhamModel
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
        public string? AnhSanPham { get; set; }
    }

    public class ThongKeDanhMucModel
    {
        public string TenDanhMuc { get; set; } = string.Empty;
        public int SoLuongSanPham { get; set; }
        public int SoLuongBan { get; set; }
        public string? AnhDanhMuc { get; set; }
    }

    public class DoanhThuThangModel
    {
        public string Thang { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
    }

    public class DanhGiaTheoSaoModel
    {
        public int SoSao { get; set; }
        public int SoLuong { get; set; }
        public double PhanTram { get; set; }
    }

    public class SanPhamDanhGiaModel
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuongDanhGia { get; set; }
        public double DiemTrungBinh { get; set; }
        public int SoSao5 { get; set; }
        public int SoSao4 { get; set; }
        public int SoSao3 { get; set; }
        public int SoSao2 { get; set; }
        public int SoSao1 { get; set; }
        public List<ChiTietDanhGiaModel> ChiTietDanhGia { get; set; } = new List<ChiTietDanhGiaModel>();
    }

    public class ChiTietDanhGiaModel
    {
        public int IdDanhGia { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public string? AnhDanhGia { get; set; }
        public DateTime? NgayDanhGia { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? TraLoiCuaShop { get; set; }
        public DateTime? NgayTraLoi { get; set; }
    }
}
