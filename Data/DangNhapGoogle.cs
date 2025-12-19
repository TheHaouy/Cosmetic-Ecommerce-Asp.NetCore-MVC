using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class DangNhapGoogle
{
    public int Id { get; set; }

    public string GoogleId { get; set; } = null!;

    public string? Email { get; set; }

    public int IdTaiKhoan { get; set; }

    public virtual TaiKhoan IdTaiKhoanNavigation { get; set; } = null!;
}
