using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using R_FACTORY_BE.Services;

namespace R_FACTORY_BE.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasPermissionAttribute : TypeFilterAttribute
{
    public string[] Permissions { get; }

    public HasPermissionAttribute(params string[] permissions) : base(typeof(HasPermissionFilter))
    {
        Permissions = permissions;
    }
}

public class HasPermissionFilter : IAuthorizationFilter
{
    private readonly IPermissionService _permissionService;
    private readonly string[] _permissions;

    public HasPermissionFilter(
        IPermissionService permissionService,
        HasPermissionAttribute attr)
    {
        _permissionService = permissionService;
        _permissions = attr.Permissions;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userIdClaim = context.HttpContext.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Unauthorized" });
            return;
        }

        var isAdminClaim = context.HttpContext.User.FindFirst("is_admin")?.Value;
        if (string.Equals(isAdminClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var userPermissions = _permissionService.GetCurrentUserPermissions(userId);
        var hasPermission = _permissions.Any(p => userPermissions.Contains(p));

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new
            {
                message = "Forbidden",
                missingPermissions = _permissions.Except(userPermissions).ToArray()
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
