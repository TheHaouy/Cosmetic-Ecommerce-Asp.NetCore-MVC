using Microsoft.AspNetCore.Mvc;
using Final_VS1.Areas.KhachHang.Services;
using System.Security.Claims;

namespace Final_VS1.Areas.KhachHang.Components
{
    public class TawkToWidgetViewComponent : ViewComponent
    {
        private readonly ITawkToService _tawkToService;
        private readonly IConfiguration _configuration;

        public TawkToWidgetViewComponent(ITawkToService tawkToService, IConfiguration configuration)
        {
            _tawkToService = tawkToService;
            _configuration = configuration;
        }

        public IViewComponentResult Invoke()
        {
            var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;
            
            // Nếu chưa đăng nhập thì không hiển thị widget
            if (!isAuthenticated)
            {
                return Content(string.Empty);
            }

            var propertyId = _configuration["Tawk:PropertyId"];
            var widgetId = _configuration["Tawk:WidgetId"];
            var apiKey = _configuration["Tawk:ApiKey"] ?? "";
            
            // Lấy thông tin người dùng từ Claims
            var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var name = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "Khách hàng";
            var avatar = HttpContext.User.FindFirst("Avatar")?.Value;
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            var userInfo = _tawkToService.GetUserInfo(email, name, avatar);
            
            // Tạo hash nếu có API Key (để bảo mật)
            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(email))
            {
                userInfo.Hash = _tawkToService.GenerateUserHash(email, apiKey);
            }

            var model = new TawkToViewModel
            {
                PropertyId = propertyId ?? "",
                WidgetId = widgetId ?? "",
                UserInfo = userInfo,
                UserId = userId,
                IsAuthenticated = isAuthenticated
            };

            return View(model);
        }
    }

    /// <summary>
    /// Model truyền dữ liệu cho View
    /// </summary>
    public class TawkToViewModel
    {
        public string PropertyId { get; set; } = string.Empty;
        public string WidgetId { get; set; } = string.Empty;
        public TawkToUserInfo UserInfo { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
    }
}
