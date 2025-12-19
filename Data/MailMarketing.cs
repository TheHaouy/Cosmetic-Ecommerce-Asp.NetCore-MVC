using System;
using System.Collections.Generic;

namespace Final_VS1.Data;

public partial class MailMarketing
{
    public int IdMail { get; set; }

    public string? NhomKhach { get; set; }

    public string NoiDung { get; set; } = null!;

    public DateTime? NgayGui { get; set; }
}
