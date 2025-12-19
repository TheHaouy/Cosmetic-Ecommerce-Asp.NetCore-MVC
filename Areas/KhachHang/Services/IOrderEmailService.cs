using System.Threading.Tasks;
using Final_VS1.Data;

namespace Final_VS1.Areas.KhachHang.Services
{
    public interface IOrderEmailService
    {
        Task SendOrderConfirmationEmailAsync(DonHang donHang);
    }
}