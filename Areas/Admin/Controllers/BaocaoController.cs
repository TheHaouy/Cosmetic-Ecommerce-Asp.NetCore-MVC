using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Models;
using Final_VS1.Areas.Admin.Models;
using Final_VS1.Data;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Final_VS1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class BaocaoController : Controller
    {
        private readonly LittleFishBeautyContext _context;

        public BaocaoController(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var baoCao = new BaocaoModel();

            // Thống kê tổng quan
            baoCao.TongSoSanPham = await _context.SanPhams.CountAsync();
            baoCao.TongSoDonHang = await _context.DonHangs.CountAsync();
            baoCao.TongSoDanhMuc = await _context.DanhMucs.CountAsync();

            // Đơn hàng theo trạng thái
            baoCao.DonHangChoXuLy = await _context.DonHangs.CountAsync(d => d.TrangThai == "Chờ xử lý");
            baoCao.DonHangDangGiao = await _context.DonHangs.CountAsync(d => d.TrangThai == "Đang giao");
            baoCao.DonHangDaGiao = await _context.DonHangs.CountAsync(d => d.TrangThai == "hoàn thành");
            baoCao.DonHangDaHuy = await _context.DonHangs.CountAsync(d => d.TrangThai == "Đã hủy");

            // Thống kê khách hàng
            baoCao.TongSoKhachHang = await _context.TaiKhoans.CountAsync(t => t.VaiTro == "Customer");

            // Khách hàng mới tháng này
            var dauThangNay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            baoCao.KhachHangMoiThangNay = await _context.TaiKhoans
                .CountAsync(t => t.VaiTro == "Customer" && t.NgayTao >= dauThangNay);

            // Khách hàng quay lại (có từ 2 đơn hàng trở lên)
            baoCao.KhachHangQuayLai = await _context.TaiKhoans
                .Where(t => t.VaiTro == "Customer")
                .CountAsync(t => t.DonHangs.Count >= 2);

            // Tỷ lệ chuyển đổi (số đơn hàng / số lượt truy cập - ước tính)
            var tongDonHang = await _context.DonHangs.CountAsync();
            var tongKhachHang = await _context.TaiKhoans.CountAsync(t => t.VaiTro == "Customer");
            baoCao.TyLeChuyenDoi = tongKhachHang > 0 ? Math.Round((double)tongDonHang / (tongKhachHang * 15) * 100, 1) : 0; // Giả sử mỗi khách truy cập 15 lần

            // Đánh giá trung bình
            var danhGiaList = await _context.DanhGia.Where(d => d.SoSao.HasValue).ToListAsync();
            baoCao.DanhGiaTrungBinh = danhGiaList.Any() ? Math.Round(danhGiaList.Average(d => d.SoSao!.Value), 1) : 0;

            // Thống kê đánh giá
            baoCao.TongSoDanhGia = await _context.DanhGia.CountAsync();
            baoCao.SanPhamCoDanhGia = await _context.SanPhams.CountAsync(s => s.DanhGia.Any());

            // Thống kê đánh giá theo số sao
            baoCao.DanhGiaTheoSao = await _context.DanhGia
                .Where(d => d.SoSao.HasValue)
                .GroupBy(d => d.SoSao!.Value)
                .Select(g => new DanhGiaTheoSaoModel
                {
                    SoSao = g.Key,
                    SoLuong = g.Count(),
                    PhanTram = 0 // Sẽ tính sau
                })
                .OrderByDescending(d => d.SoSao)
                .ToListAsync();

            // Tính phần trăm cho đánh giá theo sao
            var tongDanhGia = baoCao.DanhGiaTheoSao.Sum(d => d.SoLuong);
            foreach (var item in baoCao.DanhGiaTheoSao)
            {
                item.PhanTram = tongDanhGia > 0 ? Math.Round((double)item.SoLuong / tongDanhGia * 100, 1) : 0;
            }

            // Top sản phẩm có nhiều đánh giá nhất
            var topSanPhamQuery = await _context.SanPhams
                .Include(s => s.DanhGia)
                    .ThenInclude(d => d.IdTaiKhoanNavigation)
                .Where(s => s.DanhGia.Any())
                .ToListAsync();

            baoCao.TopSanPhamDanhGia = topSanPhamQuery
                .Select(s => new SanPhamDanhGiaModel
                {
                    TenSanPham = s.TenSanPham ?? "Không xác định",
                    SoLuongDanhGia = s.DanhGia.Count(),
                    DiemTrungBinh = s.DanhGia.Any(d => d.SoSao.HasValue)
                        ? Math.Round(s.DanhGia.Where(d => d.SoSao.HasValue).Average(d => d.SoSao!.Value), 1)
                        : 0,
                    SoSao5 = s.DanhGia.Count(d => d.SoSao == 5),
                    SoSao4 = s.DanhGia.Count(d => d.SoSao == 4),
                    SoSao3 = s.DanhGia.Count(d => d.SoSao == 3),
                    SoSao2 = s.DanhGia.Count(d => d.SoSao == 2),
                    SoSao1 = s.DanhGia.Count(d => d.SoSao == 1),
                    ChiTietDanhGia = s.DanhGia
                        .OrderByDescending(d => d.NgayDanhGia)
                        .Take(10) // Lấy 10 đánh giá mới nhất
                        .Select(d => new ChiTietDanhGiaModel
                        {
                            IdDanhGia = d.IdDanhGia,
                            TenKhachHang = d.IdTaiKhoanNavigation != null ? d.IdTaiKhoanNavigation.HoTen ?? "Khách ẩn danh" : "Khách ẩn danh",
                            SoSao = d.SoSao ?? 0,
                            BinhLuan = d.BinhLuan,
                            AnhDanhGia = d.AnhDanhGia,
                            NgayDanhGia = d.NgayDanhGia,
                            TenSanPham = s.TenSanPham ?? "Không xác định",
                            TraLoiCuaShop = d.TraLoiCuaShop,
                            NgayTraLoi = d.NgayTraLoi
                        }).ToList()
                })
                .OrderByDescending(s => s.SoLuongDanhGia)
                .Take(5)
                .ToList();

            // Đơn hàng trung bình mỗi khách
            baoCao.DonHangTrungBinhMoiKhach = tongKhachHang > 0 ? Math.Round((double)tongDonHang / tongKhachHang, 1) : 0;

            // Giá trị trung bình mỗi khách
            var tongGiaTriDonHang = await _context.DonHangs.SumAsync(d => d.TongTien ?? 0);
            baoCao.GiaTriTrungBinhMoiKhach = tongKhachHang > 0 ? Math.Round(tongGiaTriDonHang / tongKhachHang, 0) : 0;

            // Phân khúc khách hàng theo giá trị đơn hàng
            var khachHangTheoGiaTri = await _context.TaiKhoans
                .Where(t => t.VaiTro == "Customer")
                .Select(t => new
                {
                    IdTaiKhoan = t.IdTaiKhoan,
                    TongGiaTri = t.DonHangs.Where(d => d.TrangThai == "hoàn thành").Sum(d => d.TongTien ?? 0)
                })
                .ToListAsync();

            baoCao.KhachHangVIP = khachHangTheoGiaTri.Count(k => k.TongGiaTri >= 5000000); // >= 5 triệu
            baoCao.KhachHangThuong = khachHangTheoGiaTri.Count(k => k.TongGiaTri >= 1000000 && k.TongGiaTri < 5000000); // 1-5 triệu
            baoCao.KhachHangMoi = khachHangTheoGiaTri.Count(k => k.TongGiaTri < 1000000); // < 1 triệu

            // Hoạt động theo tuần (7 ngày gần nhất)
            baoCao.HoatDongTuan = new List<HoatDongTuanModel>();
            var ngayHienTai = DateTime.Now.Date;

            for (int i = 6; i >= 0; i--)
            {
                var ngay = ngayHienTai.AddDays(-i);
                var donHangTrongNgay = await _context.DonHangs.CountAsync(d => d.NgayDat.HasValue && d.NgayDat.Value.Date == ngay);

                baoCao.HoatDongTuan.Add(new HoatDongTuanModel
                {
                    NgayTrongTuan = GetTenThu(ngay.DayOfWeek),
                    SoLuongTruyCap = donHangTrongNgay * 25, // Ước tính 25 lượt truy cập cho 1 đơn hàng
                    SoDonHang = donHangTrongNgay
                });
            }

            // Top sản phẩm bán chạy
            baoCao.TopSanPhamBanChay = await _context.ChiTietDonHangs
                .Include(c => c.IdBienTheNavigation)
                    .ThenInclude(b => b!.IdSanPhamNavigation)
                        .ThenInclude(p => p!.AnhSanPhams)
                .Where(c => c.IdDonHangNavigation != null && c.IdDonHangNavigation.TrangThai == "hoàn thành")
                .Where(c => c.IdBienTheNavigation != null && c.IdBienTheNavigation.IdSanPhamNavigation != null)
                .GroupBy(c => c.IdBienTheNavigation!.IdSanPham)
                .Select(g => new TopSanPhamModel
                {
                    TenSanPham = g.First().IdBienTheNavigation!.IdSanPhamNavigation!.TenSanPham ?? "Không xác định",
                    SoLuongBan = g.Sum(c => c.SoLuong ?? 0),
                    DoanhThu = g.Sum(c => (c.SoLuong ?? 0) * (c.GiaLucDat ?? 0)),
                    AnhSanPham = g.First().IdBienTheNavigation!.IdSanPhamNavigation!.AnhSanPhams
                        .OrderBy(a => a.IdAnh)
                        .Select(a => a.LinkCloudinary ?? a.DuongDan)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToListAsync();

            // Thống kê theo danh mục
            baoCao.ThongKeDanhMuc = await _context.DanhMucs
                .Select(d => new ThongKeDanhMucModel
                {
                    TenDanhMuc = d.TenDanhMuc ?? "Không xác định",
                    AnhDanhMuc = d.AnhDaiDien,
                    SoLuongSanPham = d.SanPhams.Count(),
                    SoLuongBan = d.SanPhams
                        .SelectMany(s => s.BienTheSanPhams)
                        .SelectMany(b => b.ChiTietDonHangs)
                        .Where(c => c.IdDonHangNavigation != null && c.IdDonHangNavigation.TrangThai == "hoàn thành")
                        .Sum(c => c.SoLuong ?? 0)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .ToListAsync();

            // Doanh thu theo ngày trong tháng hiện tại
            baoCao.DoanhThu12Thang = new List<DoanhThuThangModel>();

            var thangHienTai = DateTime.Now;
            var dauThang = new DateTime(thangHienTai.Year, thangHienTai.Month, 1);
            var cuoiThang = dauThang.AddMonths(1).AddDays(-1);

            // Lấy dữ liệu theo từng ngày trong tháng
            for (var ngay = dauThang; ngay <= cuoiThang; ngay = ngay.AddDays(1))
            {
                var doanhThu = await _context.DonHangs
                    .Where(d => d.NgayDat.HasValue &&
                           d.NgayDat.Value.Date == ngay.Date &&
                           d.TrangThai == "hoàn thành")
                    .SumAsync(d => d.TongTien ?? 0);

                baoCao.DoanhThu12Thang.Add(new DoanhThuThangModel
                {
                    Thang = ngay.ToString("dd/MM"),
                    DoanhThu = doanhThu
                });
            }

            // Tổng doanh thu (tất cả đơn hàng hoàn thành)
            baoCao.TongDoanhThu = await _context.DonHangs
                .Where(d => d.TrangThai == "hoàn thành")
                .SumAsync(d => d.TongTien ?? 0);

            return View(baoCao);
        }

        // Helper method để lấy tên thứ trong tuần
        private string GetTenThu(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                DayOfWeek.Sunday => "CN",
                _ => "Unknown"
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetDoanhThuTheoNgay(DateTime tuNgay, DateTime denNgay)
        {
            var doanhThu = await _context.DonHangs
                .Where(d => d.NgayDat.HasValue && d.NgayDat >= tuNgay && d.NgayDat <= denNgay && d.TrangThai == "hoàn thành")
                .GroupBy(d => d.NgayDat!.Value.Date)
                .Select(g => new
                {
                    Ngay = g.Key.ToString("dd/MM/yyyy"),
                    DoanhThu = g.Sum(d => d.TongTien ?? 0),
                    SoDonHang = g.Count()
                })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            return Json(doanhThu);
        }

        [HttpGet]
        public async Task<IActionResult> GetThongKeSanPham()
        {
            var thongKe = await _context.SanPhams
                .Include(s => s.IdDanhMucNavigation)
                .Include(s => s.BienTheSanPhams)
                    .ThenInclude(b => b.ChiTietDonHangs)
                        .ThenInclude(c => c.IdDonHangNavigation)
                .Select(s => new
                {
                    TenSanPham = s.TenSanPham,
                    DanhMuc = s.IdDanhMucNavigation != null ? s.IdDanhMucNavigation.TenDanhMuc : "Không xác định",
                    SoLuongTon = s.BienTheSanPhams.Sum(b => b.SoLuongTonKho ?? 0),
                    SoLuongBan = s.BienTheSanPhams
                        .SelectMany(b => b.ChiTietDonHangs)
                        .Where(c => c.IdDonHangNavigation != null && c.IdDonHangNavigation.TrangThai == "hoàn thành")
                        .Sum(c => c.SoLuong ?? 0),
                    DoanhThu = s.BienTheSanPhams
                        .SelectMany(b => b.ChiTietDonHangs)
                        .Where(c => c.IdDonHangNavigation != null && c.IdDonHangNavigation.TrangThai == "hoàn thành")
                        .Sum(c => (c.SoLuong ?? 0) * (c.GiaLucDat ?? 0))
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToListAsync();

            return Json(thongKe);
        }

        [HttpGet]
        public async Task<IActionResult> GetChiTietDanhGia(
            int? idSanPham = null,
            int page = 1,
            int pageSize = 10,
            string? sanPham = null,
            int? soSao = null,
            string? hinhAnh = null,
            string sort = "newest")
        {
            var query = _context.DanhGia
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdSanPhamNavigation)
                .AsQueryable();

            // Lọc theo ID sản phẩm (từ modal)
            if (idSanPham.HasValue)
            {
                query = query.Where(d => d.IdSanPham == idSanPham.Value);
            }

            // Lọc theo tên sản phẩm
            if (!string.IsNullOrEmpty(sanPham))
            {
                query = query.Where(d => d.IdSanPhamNavigation != null && d.IdSanPhamNavigation.TenSanPham != null && d.IdSanPhamNavigation.TenSanPham.Contains(sanPham));
            }

            // Lọc theo số sao
            if (soSao.HasValue)
            {
                query = query.Where(d => d.SoSao == soSao.Value);
            }

            // Lọc theo hình ảnh
            if (!string.IsNullOrEmpty(hinhAnh))
            {
                if (hinhAnh == "co-hinh")
                {
                    query = query.Where(d => !string.IsNullOrEmpty(d.AnhDanhGia));
                }
                else if (hinhAnh == "khong-hinh")
                {
                    query = query.Where(d => string.IsNullOrEmpty(d.AnhDanhGia));
                }
            }

            // Sắp xếp
            switch (sort)
            {
                case "oldest":
                    query = query.OrderBy(d => d.NgayDanhGia);
                    break;
                case "highest":
                    query = query.OrderByDescending(d => d.SoSao).ThenByDescending(d => d.NgayDanhGia);
                    break;
                case "lowest":
                    query = query.OrderBy(d => d.SoSao).ThenByDescending(d => d.NgayDanhGia);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(d => d.NgayDanhGia);
                    break;
            }

            var totalCount = await query.CountAsync();
            var danhGia = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new ChiTietDanhGiaModel
                {
                    IdDanhGia = d.IdDanhGia,
                    TenKhachHang = d.IdTaiKhoanNavigation != null ? d.IdTaiKhoanNavigation.HoTen ?? "Khách ẩn danh" : "Khách ẩn danh",
                    SoSao = d.SoSao ?? 0,
                    BinhLuan = d.BinhLuan,
                    AnhDanhGia = d.AnhDanhGia,
                    NgayDanhGia = d.NgayDanhGia,
                    TenSanPham = d.IdSanPhamNavigation != null ? d.IdSanPhamNavigation.TenSanPham ?? "Không xác định" : "Không xác định"
                })
                .ToListAsync();

            return Json(new
            {
                data = danhGia,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                filters = new
                {
                    sanPham = sanPham,
                    soSao = soSao,
                    hinhAnh = hinhAnh,
                    sort = sort
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSanPhamCoDanhGia()
        {
            var sanPhams = await _context.DanhGia
                .Include(d => d.IdSanPhamNavigation)
                .Where(d => d.IdSanPhamNavigation != null)
                .GroupBy(d => d.IdSanPham)
                .Select(g => new
                {
                    IdSanPham = g.Key,
                    TenSanPham = g.FirstOrDefault()!.IdSanPhamNavigation!.TenSanPham,
                    SoLuongDanhGia = g.Count()
                })
                .OrderByDescending(s => s.SoLuongDanhGia)
                .ToListAsync();

            return Json(sanPhams);
        }

        // API cho bộ lọc doanh thu theo tháng
        [HttpGet]
        public async Task<IActionResult> GetDoanhThuTheoThang(int thang, int nam)
        {
            try
            {
                var firstDayOfMonth = new DateTime(nam, thang, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Khởi tạo mảng cho tất cả ngày trong tháng
                var labels = new List<string>();
                var data = new List<decimal>();

                // Lấy dữ liệu doanh thu theo ngày
                var doanhThuTheoNgay = await _context.DonHangs
                    .Where(d => d.NgayDat >= firstDayOfMonth &&
                               d.NgayDat <= lastDayOfMonth &&
                               d.TrangThai == "hoàn thành")
                    .GroupBy(d => d.NgayDat!.Value.Date)
                    .Select(g => new
                    {
                        Ngay = g.Key.Day,
                        DoanhThu = g.Sum(d => d.TongTien ?? 0)
                    })
                    .ToDictionaryAsync(x => x.Ngay, x => x.DoanhThu);

                // Tạo dữ liệu cho tất cả ngày trong tháng
                for (int day = 1; day <= lastDayOfMonth.Day; day++)
                {
                    labels.Add(day.ToString());
                    data.Add(doanhThuTheoNgay.ContainsKey(day) ? doanhThuTheoNgay[day] : 0);
                }

                return Json(new
                {
                    labels = labels,
                    data = data,
                    month = thang,
                    year = nam,
                    totalRevenue = data.Sum()
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API cho bộ lọc doanh thu theo khoảng ngày
        [HttpGet]
        public async Task<IActionResult> GetDoanhThuTheoKhoangNgay(DateTime tuNgay, DateTime denNgay)
        {
            try
            {
                var doanhThuTheoNgay = await _context.DonHangs
                    .Where(d => d.NgayDat >= tuNgay &&
                               d.NgayDat <= denNgay &&
                               d.TrangThai == "hoàn thành")
                    .GroupBy(d => d.NgayDat!.Value.Date)
                    .Select(g => new
                    {
                        Ngay = g.Key,
                        DoanhThu = g.Sum(d => d.TongTien ?? 0)
                    })
                    .OrderBy(x => x.Ngay)
                    .ToListAsync();

                return Json(doanhThuTheoNgay);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API lấy doanh thu theo quý
        [HttpGet]
        public async Task<IActionResult> GetDoanhThuTheoQuy(int nam, int quy)
        {
            try
            {
                var startMonth = (quy - 1) * 3 + 1;
                var endMonth = startMonth + 2;

                var firstDay = new DateTime(nam, startMonth, 1);
                var lastDay = new DateTime(nam, endMonth, DateTime.DaysInMonth(nam, endMonth));

                var doanhThuTheoThang = await _context.DonHangs
                    .Where(d => d.NgayDat >= firstDay &&
                               d.NgayDat <= lastDay &&
                               d.TrangThai == "hoàn thành")
                    .GroupBy(d => d.NgayDat!.Value.Month)
                    .Select(g => new
                    {
                        Thang = g.Key,
                        DoanhThu = g.Sum(d => d.TongTien ?? 0)
                    })
                    .OrderBy(x => x.Thang)
                    .ToListAsync();

                return Json(doanhThuTheoThang);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API so sánh doanh thu giữa các năm
        [HttpGet]
        public async Task<IActionResult> GetSoSanhDoanhThuTheoNam(int nam1, int nam2)
        {
            try
            {
                var doanhThuNam1 = await _context.DonHangs
                    .Where(d => d.NgayDat!.Value.Year == nam1 && d.TrangThai == "hoàn thành")
                    .GroupBy(d => d.NgayDat!.Value.Month)
                    .Select(g => new
                    {
                        Thang = g.Key,
                        DoanhThu = g.Sum(d => d.TongTien ?? 0),
                        Nam = nam1
                    })
                    .ToListAsync();

                var doanhThuNam2 = await _context.DonHangs
                    .Where(d => d.NgayDat!.Value.Year == nam2 && d.TrangThai == "hoàn thành")
                    .GroupBy(d => d.NgayDat!.Value.Month)
                    .Select(g => new
                    {
                        Thang = g.Key,
                        DoanhThu = g.Sum(d => d.TongTien ?? 0),
                        Nam = nam2
                    })
                    .ToListAsync();

                return Json(new
                {
                    Nam1 = doanhThuNam1,
                    Nam2 = doanhThuNam2
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API test để kiểm tra dữ liệu
        [HttpGet]
        public async Task<IActionResult> TestData()
        {
            try
            {
                var orders = await _context.DonHangs
                    .Select(d => new
                    {
                        ID = d.IdDonHang,
                        NgayDat = d.NgayDat,
                        TrangThai = d.TrangThai,
                        TongTien = d.TongTien
                    })
                    .Take(10)
                    .ToListAsync();

                var orderStates = await _context.DonHangs
                    .GroupBy(d => d.TrangThai)
                    .Select(g => new
                    {
                        TrangThai = g.Key,
                        SoLuong = g.Count()
                    })
                    .ToListAsync();

                return Json(new { orders, orderStates });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReplyDanhGia(int idDanhGia, string traLoi)
        {
            try
            {
                var danhGia = await _context.DanhGia.FindAsync(idDanhGia);
                if (danhGia == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                danhGia.TraLoiCuaShop = traLoi;
                danhGia.NgayTraLoi = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã trả lời đánh giá thành công",
                    ngayTraLoi = danhGia.NgayTraLoi?.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
