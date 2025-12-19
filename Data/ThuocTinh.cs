using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class ThuocTinh
{
    public int IdThuocTinh { get; set; }

    public string? TenThuocTinh { get; set; }

    public virtual ICollection<GiaTriThuocTinh> GiaTriThuocTinhs { get; set; } = new List<GiaTriThuocTinh>();
}
