using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class GioHang
{
    public int IdGioHang { get; set; }

    public int? IdTaiKhoan { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual TaiKhoan? IdTaiKhoanNavigation { get; set; }
}
