using System.ComponentModel.DataAnnotations;

namespace Final_VS1.Models
{
    public class QuenMKViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }
}
