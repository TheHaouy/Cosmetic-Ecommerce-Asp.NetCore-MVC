using Final_VS1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Final_VS1.Helper;
using Final_VS1.Areas.KhachHang.Services;
using Final_VS1.Services;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using CloudinaryDotNet;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình URL lowercase cho tất cả routes
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Cấu hình tăng giới hạn upload file cho đánh giá
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
    options.ValueLengthLimit = 104857600; // 100MB
    options.MultipartHeadersLengthLimit = 16384;
});

// Cấu hình Kestrel để tăng giới hạn request body
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100MB
});

// khai báo sử dụng DbContext với SQL Server
builder.Services.AddDbContext<LittleFishBeautyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LittleFishBeauty")));

//khai báo sử dụng cấu hình email 
builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();

// Đăng ký Mailchimp Service
builder.Services.AddScoped<IMailchimpService, MailchimpService>();

// Đăng ký VnpayService
builder.Services.AddScoped<VnpayService>();

// Đăng ký HttpContextAccessor (cần cho OrderEmailService)
builder.Services.AddHttpContextAccessor();

// Đăng ký OrderEmailService (sau IEmailSender)
builder.Services.AddScoped<IOrderEmailService, OrderEmailService>();

// Cấu hình Cloudinary
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);

// Thêm Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình ForwardedHeaders cho ngrok Hào
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
//-------------------------------

// Đăng ký TawkTo Service
builder.Services.AddSingleton<ITawkToService, TawkToService>();

// Đăng ký Google Analytics Service
builder.Services.AddSingleton<IGoogleAnalyticsService, GoogleAnalyticsService>();

// [MỚI] Cấu hình Antiforgery cho ngrok
builder.Services.AddAntiforgery(options =>
{
    // Cho phép cookie hoạt động khi chạy qua proxy/ngrok (HTTPS)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    
    // Đọc token từ header RequestVerificationToken
    options.HeaderName = "RequestVerificationToken";
});

// Cấu hình Authentication với Google
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/DangNhap/Index";
    options.LogoutPath = "/DangNhap/DangXuat";
    options.AccessDeniedPath = "/DangNhap/Index";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    
    // Yêu cầu thông tin từ Google
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Lưu token để sử dụng sau
    options.SaveTokens = true;
    
    // [FIX] Cấu hình correlation cookie cho development
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Thêm ForwardedHeaders middleware (phải đặt trước UseHttpsRedirection)
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files with proper content types
app.UseStaticFiles();

app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// SEO-friendly routes - Customer
app.MapControllerRoute(
    name: "product_slug",
    pattern: "san-pham/{slug}",
    defaults: new { area = "KhachHang", controller = "SanPham", action = "Details" });

app.MapControllerRoute(
    name: "category_slug",
    pattern: "danh-muc/{slug}",
    defaults: new { area = "KhachHang", controller = "SanPham", action = "Category" });

// SEO-friendly routes - Admin
app.MapControllerRoute(
    name: "admin_product_details",
    pattern: "admin/san-pham/{slug}",
    defaults: new { area = "Admin", controller = "Sanpham", action = "Details" });

app.MapControllerRoute(
    name: "admin_product_edit",
    pattern: "admin/san-pham/chinh-sua/{slug}",
    defaults: new { area = "Admin", controller = "Sanpham", action = "Edit" });

app.MapControllerRoute(
    name: "admin_category_details",
    pattern: "admin/danh-muc/{slug}",
    defaults: new { area = "Admin", controller = "DanhMuc", action = "Details" });

app.MapControllerRoute(
    name: "admin_category_edit",
    pattern: "admin/danh-muc/chinh-sua/{slug}",
    defaults: new { area = "Admin", controller = "DanhMuc", action = "Edit" });

// Existing routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TrangChu}/{action=Index}/{id?}",
    defaults: new { area = "KhachHang" });
    
app.MapControllerRoute(
    name: "login",
    pattern: "{controller=DangNhap}/{action=Index}/{id?}"
);

app.Run();
