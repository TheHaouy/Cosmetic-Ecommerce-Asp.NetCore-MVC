using Microsoft.AspNetCore.Mvc;
using Final_VS1.Data;
using Final_VS1.Models;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Final_VS1.Controllers
{
    public class DangNhapController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public DangNhapController(LittleFishBeautyContext context)
        {
            _context = context;
        }


        #region Đăng nhập Google
        
        [HttpGet]
        public IActionResult DangNhapGoogle(string returnUrl = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { returnUrl }),
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };
            
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = null)
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                if (!authenticateResult.Succeeded)
                {
                    TempData["Error"] = "Đăng nhập Google thất bại.";
                    return RedirectToAction("Index");
                }

                var googleId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
                var picture = authenticateResult.Principal.FindFirst("picture")?.Value;

                if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Không thể lấy thông tin từ Google.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem đã có liên kết Google chưa
                var googleLogin = await _context.DangNhapGoogles
                    .Include(g => g.IdTaiKhoanNavigation)
                    .FirstOrDefaultAsync(g => g.GoogleId == googleId);

                TaiKhoan user;

                if (googleLogin != null)
                {
                    // Đã có liên kết - đăng nhập trực tiếp
                    user = googleLogin.IdTaiKhoanNavigation;
                }
                else
                {
                    // Kiểm tra email đã tồn tại chưa
                    user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.Email == email);

                    if (user != null)
                    {
                        // Có tài khoản với email này - liên kết với Google
                        var newGoogleLogin = new DangNhapGoogle
                        {
                            GoogleId = googleId,
                            Email = email,
                            IdTaiKhoan = user.IdTaiKhoan
                        };
                        _context.DangNhapGoogles.Add(newGoogleLogin);
                    }
                    else
                    {
                        // Tạo tài khoản mới
                        user = new TaiKhoan
                        {
                            Email = email,
                            HoTen = name ?? email.Split('@')[0],
                            MatKhau = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                            VaiTro = "Khach",
                            TrangThai = true, // Tự động kích hoạt vì đã xác thực qua Google
                            NgayTao = DateTime.Now,
                            AnhDaiDien = picture
                        };
                        _context.TaiKhoans.Add(user);
                        await _context.SaveChangesAsync();

                        // Tạo liên kết Google
                        var newGoogleLogin = new DangNhapGoogle
                        {
                            GoogleId = googleId,
                            Email = email,
                            IdTaiKhoan = user.IdTaiKhoan
                        };
                        _context.DangNhapGoogles.Add(newGoogleLogin);
                    }

                    await _context.SaveChangesAsync();
                }

                // Kiểm tra trạng thái tài khoản
                if (user.TrangThai != true)
                {
                    TempData["Error"] = "Tài khoản của bạn đã bị khóa.";
                    return RedirectToAction("Index");
                }

                // Tạo claims và đăng nhập
                string normalizedRole = "Customer";
                if (!string.IsNullOrEmpty(user.VaiTro))
                {
                    var roleValue = user.VaiTro.Trim().ToLower();
                    if (roleValue == "admin" || roleValue == "administrator")
                    {
                        normalizedRole = "admin";
                    }
                    else if (roleValue == "nhanvien" || roleValue == "nhân viên" || roleValue == "staff" || roleValue == "employee")
                    {
                        // Đặt đúng chuỗi vai trò để trùng với attribute [Authorize(Roles = "Nhanvien,admin")]
                        normalizedRole = "Nhanvien";
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.IdTaiKhoan.ToString()),
                    new Claim(ClaimTypes.Name, user.HoTen ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, normalizedRole),
                    new Claim("GoogleId", googleId)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // Chuyển hướng theo vai trò
                if (normalizedRole == "admin")
                {
                    return RedirectToAction("Index", "Baocao", new { area = "Admin" });
                }
                else if (normalizedRole == "Nhanvien")
                {
                    return RedirectToAction("Index", "Trangchu", new { area = "NhanVien" });
                }
                else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "TrangChu", new { area = "KhachHang" });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        #endregion
//------------------------------------------------------------
        [HttpGet]
        public IActionResult Index(string returnUrl = null)
        {
            // Nếu đã đăng nhập, chuyển hướng đến trang phù hợp
            if (User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value?.ToLower();
                if (role == "admin")
                {
                    return RedirectToAction("Index", "Baocao", new { area = "Admin" });
                }
                else
                {
                    return RedirectToAction("Index", "TrangChu", new { area = "KhachHang" });
                }
            }

            // Lưu returnUrl để chuyển hướng sau khi đăng nhập
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DangNhapViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var user = _context.TaiKhoans
                .FirstOrDefault(u => u.Email == model.Username || u.HoTen == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhau))
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            if (user.TrangThai != true)
            {
                ViewBag.Error = "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để kích hoạt tài khoản.";
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // Chuẩn hóa vai trò - đảm bảo vai trò admin và nhân viên được xử lý đúng
            string normalizedRole = "Customer"; // Mặc định
            if (!string.IsNullOrEmpty(user.VaiTro))
            {
                var roleValue = user.VaiTro.Trim().ToLower();
                if (roleValue == "admin" || roleValue == "administrator")
                {
                    normalizedRole = "admin";
                }
                else if (roleValue == "nhanvien" || roleValue == "nhân viên" || roleValue == "staff" || roleValue == "employee")
                {
                    // Trùng với giá trị được dùng trong attribute Authorize trên controllers NhanVien
                    normalizedRole = "Nhanvien";
                }
                else if (roleValue == "khach" || roleValue == "customer")
                {
                    normalizedRole = "Customer";
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdTaiKhoan.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, normalizedRole)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // Debug log để kiểm tra vai trò
            Console.WriteLine($"[DEBUG] User {user.Email} logged in with role: {normalizedRole} (Original: {user.VaiTro})");

            // Chuyển hướng theo vai trò - ưu tiên admin và nhân viên
            if (normalizedRole == "admin")
            {
                Console.WriteLine($"[DEBUG] Redirecting admin user to Admin/Baocao");
                return RedirectToAction("Index", "Baocao", new { area = "Admin" });
            }
            else if (normalizedRole == "Nhanvien")
            {
                Console.WriteLine($"[DEBUG] Redirecting staff user to NhanVien/Trangchu");
                return RedirectToAction("Index", "Trangchu", new { area = "NhanVien" });
            }
            else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "TrangChu", new { area = "KhachHang" });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Clear session
                HttpContext.Session.Clear();
                
                // Clear any remember me cookies
                Response.Cookies.Delete("RememberMe");
                
                // Clear authentication cookie explicitly
                Response.Cookies.Delete(".AspNetCore.Cookies");
                
                return RedirectToAction("Index", "TrangChu", new { area = "KhachHang" });
            }
            catch (Exception ex)
            {
                // Log error if needed
                TempData["Error"] = "Có lỗi xảy ra khi đăng xuất. Vui lòng thử lại.";
                return RedirectToAction("Index", "ThongTin", new { area = "KhachHang" });
            }
        }
    }
}
