using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class ThanhPhan
{
    public int IdThanhPhan { get; set; }

    public string TenThanhPhan { get; set; } = null!;

    public virtual ICollection<SanPham> IdSanPhams { get; set; } = new List<SanPham>();
}
