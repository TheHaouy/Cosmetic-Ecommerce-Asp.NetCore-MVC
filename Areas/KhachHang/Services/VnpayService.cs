using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Final_VS1.Areas.KhachHang.Services
{
    public class VnpayService
    {
        private readonly IConfiguration _config;

        public VnpayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnpayRequestModel model)
        {
            // Lấy thông tin cấu hình từ appsettings.json (đã xử lý null)
            var vnp_TmnCode = _config["Vnpay:TmnCode"] ?? "";
            var vnp_HashSecret = _config["Vnpay:HashSecret"] ?? "";
            var vnp_Url = _config["Vnpay:Url"] ?? "";
            var vnp_ReturnUrl = _config["Vnpay:ReturnUrl"] ?? ""; // URL VNPAY trả về sau khi thanh toán

            // Lấy IP của khách hàng
            var vnp_IpAddr = GetIpAddress(context);

            // Tạo các tham số
            // *** Quan trọng: Chuyển Amount * 100 thành kiểu long để tránh lỗi tràn số hoặc làm tròn sai
            long amountToPay = (long)(model.Amount * 100);

            var vnp_Params = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnp_TmnCode },
                { "vnp_Amount", amountToPay.ToString() }, // Sử dụng giá trị kiểu long đã nhân 100
                { "vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", vnp_IpAddr },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", model.OrderInfo },
                { "vnp_OrderType", "other" }, // Bạn có thể tùy chỉnh
                { "vnp_ReturnUrl", vnp_ReturnUrl },
                { "vnp_TxnRef", model.OrderId.ToString() }
            };

            // Tạo chuỗi query string và thêm log chi tiết
            var queryString = new StringBuilder();
            Console.WriteLine("--- VNPAY DEBUG: Parameters Before Hashing ---"); // Log header
            foreach (var kvp in vnp_Params.OrderBy(p => p.Key)) // Đảm bảo sắp xếp đúng Alphabet
            {
                // *** THÊM LOG: Log từng tham số ***
                Console.WriteLine($"--> Param: {kvp.Key} = {kvp.Value}");

                if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value)) // Chỉ thêm nếu key và value không rỗng
                {
                    queryString.Append(kvp.Key);
                    queryString.Append("=");
                    queryString.Append(Uri.EscapeDataString(kvp.Value ?? "")); // Escape value
                    queryString.Append("&");
                }
            }
            if (queryString.Length > 0)
            {
                queryString.Length--; // Bỏ dấu & cuối cùng nếu có tham số
            }
            Console.WriteLine("---------------------------------------------"); // Log footer

            // Chuyển StringBuilder thành string để log và hash
            string finalQueryString = queryString.ToString();

            // === THÊM LOG CHI TIẾT VÀO ĐÂY ===
            Console.WriteLine("--- VNPAY DEBUG: String to Hash ---");
            Console.WriteLine(finalQueryString); // Log chuỗi cuối cùng sẽ được hash
            Console.WriteLine("--- VNPAY DEBUG: HashSecret Used ---");
            Console.WriteLine(vnp_HashSecret); // Log HashSecret đang dùng
            Console.WriteLine("-----------------------------------");
            // ===================================

            // Tạo chữ ký (đảm bảo hashSecret không null)
            var secureHash = HmacSHA512(vnp_HashSecret ?? "", finalQueryString);

            // Thêm chữ ký vào URL
            var paymentUrl = $"{vnp_Url}?{finalQueryString}&vnp_SecureHash={secureHash}";

            Console.WriteLine($"--- VNPAY DEBUG: Final Payment URL ---");
            Console.WriteLine(paymentUrl); // Log URL cuối cùng
            Console.WriteLine("-------------------------------------");

            return paymentUrl;
        }

        // Hàm kiểm tra chữ ký khi VNPAY gọi về IPN
        public bool ValidateSignature(string queryString, string inputHash, string secretKey)
        {
            // queryString nhận được từ VNPAY thường đã được URL decoded, không cần decode lại
            var checkSum = HmacSHA512(secretKey ?? "", queryString);
            bool isValid = checkSum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);

            // Thêm log để debug chữ ký IPN/Return
            Console.WriteLine($"--- VNPAY DEBUG: ValidateSignature ---");
            Console.WriteLine($"Input QueryString: {queryString}");
            Console.WriteLine($"Input Hash: {inputHash}");
            Console.WriteLine($"Secret Key Used: {secretKey ?? ""}");
            Console.WriteLine($"Calculated Checksum: {checkSum}");
            Console.WriteLine($"Is Valid: {isValid}");
            Console.WriteLine($"-------------------------------------");

            return isValid;
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            // Đảm bảo key và inputData không null
            var keyBytes = Encoding.UTF8.GetBytes(key ?? "");
            var messageBytes = Encoding.UTF8.GetBytes(inputData ?? "");

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(messageBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        private string GetIpAddress(HttpContext context)
        {
           // Luôn trả về IP localhost khi test
            return "127.0.0.1";
        }
    }

    // Model để truyền dữ liệu cho VNPAY Service
    public class VnpayRequestModel
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty; // Đã khởi tạo
        public DateTime CreatedDate { get; set; }
    }
}