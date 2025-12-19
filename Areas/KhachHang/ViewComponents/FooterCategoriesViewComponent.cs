using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Final_VS1.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Final_VS1.Areas.KhachHang.ViewComponents
{
    public class FooterCategoriesViewComponent : ViewComponent
    {
        private readonly LittleFishBeautyContext _context;

        public FooterCategoriesViewComponent(LittleFishBeautyContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy tất cả danh mục cha
            var parentCategories = await _context.DanhMucs
                .Where(d => d.IdDanhMucCha == null)
                .Include(d => d.InverseIdDanhMucChaNavigation)
                .ToListAsync();

            var categoryViewModels = new List<CategoryViewModel>();

            foreach (var parent in parentCategories)
            {
                // Lấy danh sách ID của danh mục cha và tất cả danh mục con
                var categoryIds = new List<int> { parent.IdDanhMuc };
                categoryIds.AddRange(parent.InverseIdDanhMucChaNavigation.Select(c => c.IdDanhMuc));

                // Đếm tổng số lượng sản phẩm từ danh mục cha và tất cả danh mục con
                var totalProducts = await _context.SanPhams
                    .Where(s => s.IdDanhMuc.HasValue && categoryIds.Contains(s.IdDanhMuc.Value) && s.TrangThai == true)
                    .CountAsync();

                // Lấy danh sách danh mục con với số lượng sản phẩm
                var subCategories = new List<SubCategoryViewModel>();
                foreach (var sub in parent.InverseIdDanhMucChaNavigation.OrderBy(s => s.ThuTuHienThi ?? 0))
                {
                    var subProductCount = await _context.SanPhams
                        .Where(s => s.IdDanhMuc == sub.IdDanhMuc && s.TrangThai == true)
                        .CountAsync();

                    subCategories.Add(new SubCategoryViewModel
                    {
                        IdDanhMuc = sub.IdDanhMuc,
                        TenDanhMuc = sub.TenDanhMuc ?? "",
                        SoLuongSanPham = subProductCount
                    });
                }

                categoryViewModels.Add(new CategoryViewModel
                {
                    IdDanhMuc = parent.IdDanhMuc,
                    TenDanhMuc = parent.TenDanhMuc ?? "",
                    SoLuongBan = totalProducts,
                    SubCategories = subCategories
                });
            }

            // Sắp xếp theo số lượng sản phẩm và lấy top 6
            var topCategories = categoryViewModels
                .OrderByDescending(x => x.SoLuongBan)
                .Take(6)
                .ToList();

            return View(topCategories);
        }
    }

    public class CategoryViewModel
    {
        public int IdDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public int SoLuongBan { get; set; }
        public List<SubCategoryViewModel> SubCategories { get; set; } = new List<SubCategoryViewModel>();
    }

    public class SubCategoryViewModel
    {
        public int IdDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public int SoLuongSanPham { get; set; }
    }
}
