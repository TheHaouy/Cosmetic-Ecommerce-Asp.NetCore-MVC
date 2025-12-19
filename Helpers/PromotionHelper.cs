using Final_VS1.Data;
using Microsoft.EntityFrameworkCore;

namespace Final_VS1.Helpers
{
    /// <summary>
    /// Helper để xử lý logic khuyến mãi
    /// </summary>
    public static class PromotionHelper
    {
        /// <summary>
        /// Lấy khuyến mãi tốt nhất cho sản phẩm
        /// </summary>
        public static async Task<KhuyenMai?> GetBestPromotionForProduct(
            LittleFishBeautyContext context, 
            int productId,
            int? categoryId = null)
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            var promotions = await context.KhuyenMais
                .Include(k => k.KhuyenMaiSanPhams)
                .Include(k => k.KhuyenMaiDanhMucs)
                .Where(k => k.TrangThai == "DANG_HOAT_DONG" &&
                           k.NgayBatDau <= now &&
                           k.NgayKetThuc >= now)
                .ToListAsync();

            // Lọc theo flash sale time nếu có
            promotions = promotions
                .Where(k => !k.GioBatDau.HasValue || 
                           (k.GioBatDau.Value <= currentTime && k.GioKetThuc.Value >= currentTime))
                .ToList();

            // Lọc theo sản phẩm/danh mục
            var validPromotions = promotions.Where(k =>
            {
                // Kiểm tra sản phẩm cụ thể
                if (k.KhuyenMaiSanPhams.Any(sp => sp.IdSanPham == productId))
                {
                    // Kiểm tra số lượng còn lại
                    var kmSanPham = k.KhuyenMaiSanPhams.First(sp => sp.IdSanPham == productId);
                    if (k.SoLuongGioiHan.HasValue && k.SoLuongDaBan >= k.SoLuongGioiHan.Value)
                        return false;
                    
                    return true;
                }

                // Kiểm tra danh mục
                if (categoryId.HasValue && k.KhuyenMaiDanhMucs.Any(dm => dm.IdDanhMuc == categoryId.Value))
                {
                    if (k.SoLuongGioiHan.HasValue && k.SoLuongDaBan >= k.SoLuongGioiHan.Value)
                        return false;
                    
                    return true;
                }

                // Kiểm tra không có sản phẩm/danh mục cụ thể (áp dụng cho tất cả)
                if (!k.KhuyenMaiSanPhams.Any() && !k.KhuyenMaiDanhMucs.Any())
                {
                    if (k.SoLuongGioiHan.HasValue && k.SoLuongDaBan >= k.SoLuongGioiHan.Value)
                        return false;
                    
                    return true;
                }

                return false;
            }).ToList();

            // Chọn khuyến mãi có ưu tiên cao nhất
            return validPromotions.OrderByDescending(k => k.UuTien).FirstOrDefault();
        }

        /// <summary>
        /// Tính giá sau khuyến mãi
        /// </summary>
        public static decimal CalculatePromotionPrice(decimal originalPrice, KhuyenMai promotion)
        {
            if (originalPrice <= 0 || promotion == null)
                return originalPrice;

            decimal promotionPrice = originalPrice;

            switch (promotion.HinhThucGiam)
            {
                case "PHAN_TRAM":
                    var discount = originalPrice * promotion.GiaTriGiam / 100;
                    
                    // Áp dụng giảm tối đa nếu có
                    if (promotion.GiaTriGiamToiDa.HasValue && discount > promotion.GiaTriGiamToiDa.Value)
                    {
                        discount = promotion.GiaTriGiamToiDa.Value;
                    }
                    
                    promotionPrice = originalPrice - discount;
                    break;

                case "SO_TIEN":
                    promotionPrice = originalPrice - promotion.GiaTriGiam;
                    break;

                case "GIA_CO_DINH":
                    promotionPrice = promotion.GiaTriGiam;
                    break;
            }

            // Đảm bảo giá không âm
            return promotionPrice > 0 ? promotionPrice : 0;
        }

        /// <summary>
        /// Tính % giảm giá
        /// </summary>
        public static decimal CalculateDiscountPercentage(decimal originalPrice, decimal promotionPrice)
        {
            if (originalPrice <= 0) return 0;
            
            var discount = originalPrice - promotionPrice;
            return Math.Round((discount / originalPrice) * 100, 0);
        }

        /// <summary>
        /// Kiểm tra khuyến mãi có hợp lệ không
        /// </summary>
        public static bool IsPromotionValid(KhuyenMai promotion)
        {
            if (promotion == null) return false;
            
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;

            // Kiểm tra trạng thái
            if (promotion.TrangThai != "DANG_HOAT_DONG")
                return false;

            // Kiểm tra thời gian
            if (promotion.NgayBatDau > now || promotion.NgayKetThuc < now)
                return false;

            // Kiểm tra flash sale time
            if (promotion.GioBatDau.HasValue && promotion.GioKetThuc.HasValue)
            {
                if (currentTime < promotion.GioBatDau.Value || currentTime > promotion.GioKetThuc.Value)
                    return false;
            }

            // Kiểm tra số lượng
            if (promotion.SoLuongGioiHan.HasValue && promotion.SoLuongDaBan >= promotion.SoLuongGioiHan.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Lấy text hiển thị cho loại khuyến mãi
        /// </summary>
        public static string GetPromotionTypeText(string loaiKhuyenMai)
        {
            return loaiKhuyenMai switch
            {
                "GIAM_GIA_SAN_PHAM" => "Giảm giá sản phẩm",
                "GIAM_GIA_DON_HANG" => "Giảm giá đơn hàng",
                "MA_GIAM_GIA" => "Mã giảm giá",
                "FREESHIP" => "Freeship",
                "TANG_QUA" => "Tặng quà",
                "COMBO" => "Combo sản phẩm",
                _ => loaiKhuyenMai
            };
        }

        /// <summary>
        /// Lấy text hiển thị cho giá trị giảm
        /// </summary>
        public static string GetDiscountText(KhuyenMai promotion)
        {
            if (promotion == null) return "";

            return promotion.HinhThucGiam switch
            {
                "PHAN_TRAM" => $"-{promotion.GiaTriGiam:0.#}%",
                "SO_TIEN" => $"-{promotion.GiaTriGiam:N0}đ",
                "GIA_CO_DINH" => $"Giá mới: {promotion.GiaTriGiam:N0}đ",
                _ => ""
            };
        }

        /// <summary>
        /// Tính thời gian còn lại của khuyến mãi
        /// </summary>
        public static string GetTimeRemaining(KhuyenMai promotion)
        {
            if (promotion == null) return "";

            var now = DateTime.Now;
            var remaining = promotion.NgayKetThuc - now;

            if (remaining.TotalDays >= 1)
            {
                return $"Còn {Math.Floor(remaining.TotalDays)} ngày";
            }
            else if (remaining.TotalHours >= 1)
            {
                return $"Còn {Math.Floor(remaining.TotalHours)} giờ";
            }
            else if (remaining.TotalMinutes >= 1)
            {
                return $"Còn {Math.Floor(remaining.TotalMinutes)} phút";
            }
            else
            {
                return "Sắp hết hạn";
            }
        }

        /// <summary>
        /// Lấy badge class cho khuyến mãi
        /// </summary>
        public static string GetPromotionBadgeClass(KhuyenMai promotion)
        {
            if (promotion == null) return "bg-secondary";

            // Flash sale
            if (promotion.GioBatDau.HasValue && promotion.GioKetThuc.HasValue)
                return "bg-danger";

            // Giảm giá cao
            if (promotion.HinhThucGiam == "PHAN_TRAM" && promotion.GiaTriGiam >= 50)
                return "bg-danger";

            // Giảm giá trung bình
            if (promotion.HinhThucGiam == "PHAN_TRAM" && promotion.GiaTriGiam >= 30)
                return "bg-warning";

            return "bg-success";
        }

        /// <summary>
        /// Kiểm tra sản phẩm có đang trong flash sale không
        /// </summary>
        public static bool IsFlashSale(KhuyenMai? promotion)
        {
            if (promotion == null) return false;
            
            return promotion.GioBatDau.HasValue && 
                   promotion.GioKetThuc.HasValue && 
                   promotion.SoLuongGioiHan.HasValue;
        }

        /// <summary>
        /// Lấy % còn lại của flash sale
        /// </summary>
        public static int GetFlashSaleRemainingPercent(KhuyenMai promotion)
        {
            if (!promotion.SoLuongGioiHan.HasValue || promotion.SoLuongGioiHan.Value == 0)
                return 100;

            var remaining = promotion.SoLuongGioiHan.Value - promotion.SoLuongDaBan;
            var percent = (remaining * 100) / promotion.SoLuongGioiHan.Value;
            
            return Math.Max(0, Math.Min(100, percent));
        }
    }
}
