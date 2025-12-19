using Microsoft.AspNetCore.Mvc;
using Final_VS1.Data;
using Final_VS1.Models;
using Final_VS1.Helper;
using Microsoft.EntityFrameworkCore;

namespace Final_VS1.Controllers
{
    public class Quen_MKController : Controller
    {
        private readonly LittleFishBeautyContext _context;
        private readonly IEmailSender _emailSender;

        public Quen_MKController(LittleFishBeautyContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(QuenMKViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra email có tồn tại trong hệ thống không
                var user = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    TempData["Error"] = "Email này không tồn tại trong hệ thống";
                    return View(model);
                }

                // Tạo mã xác nhận ngẫu nhiên
                var resetCode = GenerateResetCode();
                var expireTime = DateTime.Now.AddMinutes(15); // Mã có hiệu lực 15 phút

                // Lưu mã xác nhận vào session hoặc database
                HttpContext.Session.SetString($"ResetCode_{model.Email}", resetCode);
                HttpContext.Session.SetString($"ResetCodeExpire_{model.Email}", expireTime.ToString());
                HttpContext.Session.SetString($"ResetEmail", model.Email);

                // Tạo link xác nhận
                var resetLink = Url.Action("Index", "Doi_MK", 
                    new { email = model.Email, code = resetCode }, 
                    Request.Scheme);

                // Nội dung email
                var emailSubject = "Xác nhận đổi mật khẩu - Little Fish Beauty";
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #d8f3dc 0%, #74c69d 100%); padding: 30px; border-radius: 10px; text-align: center;'>
                            <h2 style='color: #2d5016; margin: 0;'>🌿 Little Fish Beauty 🌿</h2>
                        </div>
                        
                        <div style='background: #ffffff; padding: 30px; border-radius: 10px; margin-top: 20px; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                            <h3 style='color: #2d5016; margin-bottom: 20px;'>Xác nhận đổi mật khẩu</h3>
                            
                            <p style='color: #333; line-height: 1.6; margin-bottom: 20px;'>
                                Chào bạn,<br>
                                Chúng tôi nhận được yêu cầu đổi mật khẩu cho tài khoản của bạn.
                            </p>
                            
                            <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <p style='margin: 0; color: #666;'><strong>Mã xác nhận:</strong></p>
                                <p style='font-size: 24px; font-weight: bold; color: #2d5016; margin: 10px 0; letter-spacing: 2px;'>{resetCode}</p>
                                <p style='margin: 0; color: #999; font-size: 14px;'>Mã có hiệu lực trong 15 phút</p>
                            </div>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='background: linear-gradient(45deg, #2d5016, #4a7c59); color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; display: inline-block; font-weight: bold; box-shadow: 0 4px 12px rgba(45, 80, 22, 0.3);'>
                                    🔐 Đổi mật khẩu ngay
                                </a>
                            </div>
                            
                            <p style='color: #666; font-size: 14px; line-height: 1.6;'>
                                Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.<br>
                                Để bảo mật tài khoản, không chia sẻ mã xác nhận với bất kỳ ai.
                            </p>
                        </div>
                        
                        <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
                            <p>© 2024 Little Fish Beauty - Làm đẹp tự nhiên</p>
                        </div>
                    </div>";

                // Gửi email
                await _emailSender.SenderEmailAsync(model.Email, emailSubject, emailBody);

                TempData["Success"] = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư!";
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại!";
                return View(model);
            }
        }

        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Mã 6 số
        }
    }
}
