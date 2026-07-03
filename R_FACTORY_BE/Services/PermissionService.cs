using Microsoft.Extensions.Caching.Memory;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetCurrentUserPermissionsAsync(int userId);
    HashSet<string> GetCurrentUserPermissions(int userId);
}

public class PermissionService : IPermissionService
{
    private readonly IGenericRepo _repo;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PermissionService(IGenericRepo repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task<List<PermissionDto>> GetCurrentUserPermissionsAsync(int userId)
    {
        var cacheKey = $"user_permissions_{userId}";

        if (_cache.TryGetValue(cacheKey, out List<PermissionDto>? cached) && cached != null)
        {
            return cached;
        }

        var permissions = await _repo.ProcedureToList<PermissionDto>(
            "spPermission",
            ["p_UserId"],
            [userId]
        );

        _cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }

    public HashSet<string> GetCurrentUserPermissions(int userId)
    {
        var cacheKey = $"user_permissions_{userId}";

        if (_cache.TryGetValue(cacheKey, out List<PermissionDto>? cached) && cached != null)
        {
            return cached.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public void InvalidateCache(int userId)
    {
        var cacheKey = $"user_permissions_{userId}";
        _cache.Remove(cacheKey);
    }
}
