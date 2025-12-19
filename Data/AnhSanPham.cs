using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class AnhSanPham
{
    public int IdAnh { get; set; }

    public int? IdSanPham { get; set; }

    public string? DuongDan { get; set; }

    public string? LoaiAnh { get; set; }

    public string? LinkCloudinary { get; set; }

    public virtual SanPham? IdSanPhamNavigation { get; set; }
}
