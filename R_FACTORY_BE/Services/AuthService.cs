using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;

namespace R_FACTORY_BE.Services;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, string? deviceInfo);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<TokenResponse?> RefreshAsync(string refreshToken, string? deviceInfo);
    Task LogoutAsync(int userId, string? refreshToken, string? ipAddress);
}

public class AuthService : IAuthService
{
    private readonly IGenericRepo _repo;
    private readonly ITokenService _tokenService;

    public AuthService(IGenericRepo repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, string? deviceInfo)
    {
        var matchedUser = await _repo.FindModel<User>(u => u.Username == request.Username);

        if (matchedUser == null || matchedUser.IsActive != true)
            return null;

        if (!VerifyPassword(request.Password, matchedUser.PasswordHash))
            return null;

        Console.WriteLine($"[DEBUG Login] User {matchedUser.Username}: IsActive={matchedUser.IsActive}, IsAdmin={matchedUser.IsAdmin}");

        var roles = await GetUserRolesAsync(matchedUser.Id);
        var department = matchedUser.DepartmentId.HasValue
            ? await GetDepartmentNameAsync(matchedUser.DepartmentId.Value)
            : null;

        var accessToken = _tokenService.GenerateAccessToken(matchedUser, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.SaveRefreshTokenAsync(matchedUser.Id, refreshToken, deviceInfo);

        Console.WriteLine($"[DEBUG Login] Returning IsAdmin={matchedUser.IsAdmin == true}");

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 30 * 60,
            User = new UserDto
            {
                Id = matchedUser.Id,
                Username = matchedUser.Username,
                FullName = matchedUser.FullName,
                Email = matchedUser.Email,
                PhoneNumber = matchedUser.PhoneNumber,
                DepartmentName = department,
                IsAdmin = matchedUser.IsAdmin == true,
                Roles = roles
            }
        };
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, string? deviceInfo)
    {
        var token = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
        if (token == null) return null;

        var user = await _repo.FindModel<User>(u => u.Id == token.UserId);
        if (user == null || user.IsActive != true) return null;

        var roles = await GetUserRolesAsync(user.Id);
        var department = user.DepartmentId.HasValue
            ? await GetDepartmentNameAsync(user.DepartmentId.Value)
            : null;

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.RotateRefreshTokenAsync(token, newRefreshToken, deviceInfo ?? "unknown");

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = 30 * 60,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DepartmentName = department,
                IsAdmin = user.IsAdmin == true,
                Roles = roles
            }
        };
    }

    public async Task LogoutAsync(int userId, string? refreshToken, string? ipAddress)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var token = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
            if (token != null)
            {
                await _tokenService.RevokeRefreshTokenAsync(token, ipAddress ?? "unknown", "logout");
            }
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var matchedUser = await _repo.FindModel<User>(u => u.Id == userId);
        if (matchedUser == null) return null;

        var roles = await GetUserRolesAsync(matchedUser.Id);
        var department = matchedUser.DepartmentId.HasValue
            ? await GetDepartmentNameAsync(matchedUser.DepartmentId.Value)
            : null;

        return new UserDto
        {
            Id = matchedUser.Id,
            Username = matchedUser.Username,
            FullName = matchedUser.FullName,
            Email = matchedUser.Email,
            PhoneNumber = matchedUser.PhoneNumber,
            DepartmentName = department,
            IsAdmin = matchedUser.IsAdmin == true,
            Roles = roles
        };
    }

    private async Task<List<string>> GetUserRolesAsync(int userId)
    {
        var roleNames = new List<string>();
        return roleNames;
    }

    private async Task<string?> GetDepartmentNameAsync(int departmentId)
    {
        var dept = await _repo.FindModel<Department>(d => d.Id == departmentId);
        return dept?.DepartmentName;
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
