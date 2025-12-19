using System.Security.Cryptography;
using System.Text;

namespace Final_VS1.Areas.KhachHang.Services
{
    public interface ITawkToService
    {
        string GenerateUserHash(string email, string apiKey);
        TawkToUserInfo GetUserInfo(string email, string hoTen, string? anhDaiDien);
    }

    public class TawkToService : ITawkToService
    {
        /// <summary>
        /// Tạo hash để xác thực người dùng với Tawk.to
        /// </summary>
        public string GenerateUserHash(string email, string apiKey)
        {
            var encoding = new UTF8Encoding();
            var keyBytes = encoding.GetBytes(apiKey);
            var messageBytes = encoding.GetBytes(email);
            
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Lấy thông tin người dùng để hiển thị trong Tawk.to
        /// </summary>
        public TawkToUserInfo GetUserInfo(string email, string hoTen, string? anhDaiDien)
        {
            return new TawkToUserInfo
            {
                Name = hoTen,
                Email = email,
                Avatar = anhDaiDien,
                Hash = string.Empty
            };
        }
    }

    /// <summary>
    /// Model chứa thông tin người dùng cho Tawk.to
    /// </summary>
    public class TawkToUserInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}
