using Final_VS1.Data;

namespace Final_VS1.Areas.NhanVien.Models
{
    public class DashboardViewModel
    {
        public int DonHangChoXuLy { get; set; }
        public int DonHangDangGiao { get; set; }
        public int DonHangHoanThanh { get; set; }
        public int SanPhamSapHet { get; set; }
        public List<DonHang> DonHangMoiNhat { get; set; } = new List<DonHang>();
        public List<BienTheSanPham> SanPhamSapHetChiTiet { get; set; } = new List<BienTheSanPham>();
    }
}
