using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Final_VS1.Data;
using Final_VS1.Helper; // Namespace chứa IEmailSender
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration; // [QUAN TRỌNG] Thêm namespace này

namespace Final_VS1.Areas.KhachHang.Services
{
    public class OrderEmailService : IOrderEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl; // Biến lưu domain công khai

        // Inject thêm IConfiguration
        public OrderEmailService(IEmailSender emailSender, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;

            // Lấy BaseUrl từ appsettings.json. Nếu chưa cấu hình thì fallback về chuỗi rỗng
            // Lưu ý: BaseUrl trong appsettings phải là link ngrok (https://....ngrok-free.app) hoặc domain thật
            _baseUrl = config["SiteSettings:BaseUrl"] ?? "";
            
            // Xử lý xóa dấu gạch chéo ở cuối nếu có để tránh trùng (domain.com//images)
            if (_baseUrl.EndsWith("/")) _baseUrl = _baseUrl.Substring(0, _baseUrl.Length - 1);
        }

        public async Task SendOrderConfirmationEmailAsync(DonHang donHang)
        {
            var userEmail = donHang.IdTaiKhoanNavigation?.Email;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                Console.WriteLine($"Warning: Không tìm thấy email cho đơn hàng #{donHang.IdDonHang}");
                return;
            }

            string subject = $"Xác nhận đơn hàng #{donHang.IdDonHang} - Thanh toán thành công";
            
            // Dictionary chứa: Key = CID (product-image-1), Value = Full URL (https://ngrok.../images/a.jpg)
            var imageUrls = new Dictionary<string, string>();
            
            string htmlMessage = GetEmailBody(donHang, imageUrls);

            try 
            {
                // Gọi hàm gửi mail (Hàm này sẽ tự động tải ảnh từ URL và đính kèm vào mail với CID tương ứng)
                await _emailSender.SenderEmailAsync(userEmail, subject, htmlMessage, imageUrls);
                Console.WriteLine($"Đã gửi email hóa đơn tới: {userEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            }
        }

        private string GetEmailBody(DonHang donHang, Dictionary<string, string> imageUrls)
        {
            string itemsHtml = "";
            int imageIndex = 0;
            
            if (donHang.ChiTietDonHangs != null)
            {
                foreach (var item in donHang.ChiTietDonHangs)
                {
                    var tenSp = item.IdBienTheNavigation?.IdSanPhamNavigation?.TenSanPham ?? "Sản phẩm";
                    var gia = item.GiaLucDat?.ToString("N0") + " đ";
                    var thanhTien = (item.GiaLucDat * item.SoLuong)?.ToString("N0") + " đ";
                    
                    // Lấy đường dẫn ảnh gốc từ DB
                    var anhSanPham = item.IdBienTheNavigation?.IdSanPhamNavigation?.AnhSanPhams?.FirstOrDefault();
                    string rawPath = anhSanPham?.DuongDan;

                    string imageCid;
                    string fullImageUrl;

                    if (!string.IsNullOrEmpty(rawPath))
                    {
                        // [XỬ LÝ URL ẢNH QUAN TRỌNG]
                        // 1. Nếu là link Cloudinary/Online (bắt đầu bằng http) -> Giữ nguyên
                        if (rawPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            fullImageUrl = rawPath;
                        }
                        else 
                        {
                            // 2. Nếu là link nội bộ (/images/...) -> Ghép với BaseUrl (ngrok)
                            if (!rawPath.StartsWith("/")) rawPath = "/" + rawPath;
                            fullImageUrl = $"{_baseUrl}{rawPath}";
                        }

                        // Tạo Content-ID
                        imageCid = $"product-image-{imageIndex}";
                        imageUrls[imageCid] = fullImageUrl; // Lưu Full URL vào dictionary
                        imageIndex++;
                    }
                    else
                    {
                        // Placeholder
                        imageCid = "placeholder";
                        if (!imageUrls.ContainsKey(imageCid))
                        {
                            imageUrls[imageCid] = "https://via.placeholder.com/100x100/e0e0e0/666666?text=No+Image";
                        }
                    }
                    
                    // ... (Phần lấy thuộc tính giữ nguyên) ...
                    string thuocTinhHtml = "";
                    if (item.IdBienTheNavigation?.IdGiaTris != null && item.IdBienTheNavigation.IdGiaTris.Any())
                    {
                        var thuocTinhList = item.IdBienTheNavigation.IdGiaTris
                            .Select(gt => $"{gt.IdThuocTinhNavigation?.TenThuocTinh}: {gt.GiaTri}")
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        
                        if (thuocTinhList.Any())
                        {
                            thuocTinhHtml = $"<div style='font-size: 12px; color: #666; margin-top: 5px;'>{string.Join(" | ", thuocTinhList)}</div>";
                        }
                    }
                    
                    itemsHtml += $@"
                        <tr>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; width: 50%;'>
                                <div style='display: flex; align-items: center;'>
                                    <img src='cid:{imageCid}' alt='Img' style='width: 80px; height: 80px; object-fit: cover; border-radius: 4px; margin-right: 15px;' />
                                    <div>
                                        <div style='font-weight: 500;'>{tenSp}</div>
                                        {thuocTinhHtml}
                                    </div>
                                </div>
                            </td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: center; width: 10%; white-space: nowrap;'>{item.SoLuong}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right; width: 20%; white-space: nowrap;'>{gia}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right; width: 20%; white-space: nowrap;'>{thanhTien}</td>
                        </tr>";
                }
            }

            // ... (Phần HTML trả về giữ nguyên) ...
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #2d7b2c; padding: 20px; text-align: center; color: white;'>
                        <h2 style='margin: 0;'>Cảm ơn bạn đã mua hàng!</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <p>Xin chào <strong>{donHang.IdTaiKhoanNavigation?.HoTen ?? "Khách hàng"}</strong>,</p>
                        <p>Đơn hàng <strong>#DH{donHang.IdDonHang:D6}</strong> của bạn đã được thanh toán thành công qua <strong>{donHang.PhuongThucThanhToan}</strong>.</p>
                        
                        <h3 style='border-bottom: 2px solid #2d7b2c; padding-bottom: 10px; margin-top: 20px;'>Chi tiết đơn hàng</h3>
                        <table style='width: 100%; border-collapse: collapse; table-layout: fixed;'>
                            <thead>
                                <tr style='background-color: #f9f9f9;'>
                                    <th style='padding: 10px; text-align: left; width: 50%;'>Sản phẩm</th>
                                    <th style='padding: 10px; text-align: center; width: 10%;'>SL</th>
                                    <th style='padding: 10px; text-align: right; width: 20%;'>Giá</th>
                                    <th style='padding: 10px; text-align: right; width: 20%;'>Thành tiền</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemsHtml}
                            </tbody>
                            <tfoot>
                                <tr>
                                    <td colspan='3' style='padding: 10px; text-align: right; font-weight: bold;'>Tổng cộng:</td>
                                    <td style='padding: 10px; text-align: right; font-weight: bold; color: #d32f2f; white-space: nowrap;'>{donHang.TongTien?.ToString("N0")} đ</td>
                                </tr>
                            </tfoot>
                        </table>

                        <p style='margin-top: 30px;'>Chúng tôi sẽ sớm giao hàng đến địa chỉ của bạn.</p>
                        <p>Trân trọng,<br/>Đội ngũ LittleFish Beauty</p>
                    </div>
                </div>";
        }
    }
}