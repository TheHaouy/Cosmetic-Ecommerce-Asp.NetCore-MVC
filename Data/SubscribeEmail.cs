using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class SubscribeEmail
{
    public int IdSub { get; set; }

    public string Email { get; set; } = null!;

    public DateTime? NgayDangKy { get; set; }
}
