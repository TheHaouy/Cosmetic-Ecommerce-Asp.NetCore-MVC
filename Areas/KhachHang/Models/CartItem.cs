namespace Final_VS1.Areas.KhachHang.Models
{
    public class CartItem
    {
        public int? IdSanPham { get; set; }
        public int? IdBienThe { get; set; }
        public string? TenSanPham { get; set; }
        public string? TenBienThe { get; set; }
        public string? LinkAnh { get; set; }
        public decimal Gia { get; set; }
        public int SoLuong { get; set; } = 1;
        public Dictionary<string, string>? ThuocTinhs { get; set; }
    }
}
