using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Services;

public class DataSeeder
{
    private readonly IGenericRepo _repo;

    public DataSeeder(IGenericRepo repo)
    {
        _repo = repo;
    }

    public async Task SeedMenusAsync()
    {
        var existingMenus = await _repo.GetAll<Menu>();
        if (existingMenus.Any()) return;

        var now = DateTime.Now;
        var menus = new List<Menu>
        {
            // Parent: Factory System
            new() { Name = "Hệ thống nhà máy", Url = "", Icon = "pi-building", Order = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },

            // Children: Factory System (ParentId = 1)
            new() { Name = "Quản lý nhà máy", Url = "/factories", Icon = "pi-building", Order = 1, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Quản lý khu vực", Url = "/areas", Icon = "pi-map", Order = 2, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Quản lý dây chuyền", Url = "/lines", Icon = "pi-share-alt", Order = 3, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Quản lý máy móc", Url = "/machines", Icon = "pi-cog", Order = 4, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Loại máy", Url = "/machine-types", Icon = "pi-tags", Order = 5, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Quản lý mẫu sản phẩm", Url = "/models", Icon = "pi-box", Order = 6, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Thời gian chu kỳ máy", Url = "/model-machine-cycle-times", Icon = "pi-stopwatch", Order = 7, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Quản lý bố trí", Url = "/layouts", Icon = "pi-sitemap", Order = 8, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Ca làm việc", Url = "/shifts", Icon = "pi-clock", Order = 9, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Loại dừng planned", Url = "/planned-downtime-types", Icon = "pi-calendar-times", Order = 10, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Lịch dừng planned", Url = "/planned-downtime-schedules", Icon = "pi-calendar", Order = 11, ParentMenuId = 1, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },

            // Parent: OEE Module
            new() { Name = "Module OEE", Url = "", Icon = "pi-chart-line", Order = 2, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },

            // Children: OEE Module (ParentId = 13)
            new() { Name = "Lỗi máy", Url = "/errors", Icon = "pi-exclamation-triangle", Order = 1, ParentMenuId = 13, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Lịch sử máy", Url = "/machine-history", Icon = "pi-history", Order = 2, ParentMenuId = 13, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Dashboard", Url = "/dashboard", Icon = "pi-chart-line", Order = 3, ParentMenuId = 13, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },

            // Parent: Administration
            new() { Name = "Quản trị", Url = "", Icon = "pi-sitemap", Order = 3, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },

            // Children: Administration (ParentId = 17)
            new() { Name = "Phòng ban", Url = "/departments", Icon = "pi-sitemap", Order = 1, ParentMenuId = 17, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Người dùng", Url = "/users", Icon = "pi-user", Order = 2, ParentMenuId = 17, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Vai trò", Url = "/roles", Icon = "pi-id-card", Order = 3, ParentMenuId = 17, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Phân quyền", Url = "/permissions", Icon = "pi-key", Order = 4, ParentMenuId = 17, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
            new() { Name = "Menu", Url = "/menus", Icon = "pi-bars", Order = 5, ParentMenuId = 17, IsDeleted = false, CreatedDate = now, CreatedBy = "system", UpdatedDate = null, UpdatedBy = "" },
        };

        foreach (var menu in menus)
        {
            await _repo.Insert(menu);
        }
    }
}
