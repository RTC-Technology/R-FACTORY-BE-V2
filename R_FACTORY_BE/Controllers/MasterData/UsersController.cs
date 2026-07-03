using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.Models.Models;
using R_FACTORY_BE.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace R_FACTORY_BE.Controllers.MasterData;

[ApiController]
[Route("api/users")]
public class UsersController : CrudControllerBase<User>
{
    public UsersController(IGenericRepo repo) : base(repo) { }

    [HttpPost]
    public new async Task<IActionResult> Create([FromBody] UserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { Message = "Password is required." });

        if (dto.Password.Length < 6)
            return BadRequest(new { Message = "Password must be at least 6 characters." });

        var existing = await Repo.FindModel<User>(u => u.Username == dto.Username);
        if (existing != null)
            return BadRequest(new { Message = "Username already exists." });

        if (dto.DepartmentId.HasValue)
        {
            var dept = await Repo.GetById<Department>(dto.DepartmentId.Value);
            if (dept is null)
                return BadRequest(new { Message = "Department not found." });
        }

        var user = new User
        {
            Username = dto.Username,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DepartmentId = dto.DepartmentId,
            IsActive = dto.IsActive ?? true,
            IsAdmin = dto.IsAdmin ?? false,
            PasswordHash = HashPassword(dto.Password)
        };

        SetCreatedTimestamp(user);
        var saved = await Repo.Insert(user);
        return Ok(saved);
    }

    [HttpPut("{id:int}")]
    public new async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
    {
        var existing = await Repo.GetById<User>(id);
        if (existing is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 6)
            return BadRequest(new { Message = "Password must be at least 6 characters." });

        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != existing.Username)
        {
            var duplicate = await Repo.FindModel<User>(u => u.Username == dto.Username && u.Id != id);
            if (duplicate != null)
                return BadRequest(new { Message = "Username already exists." });
        }

        if (dto.DepartmentId.HasValue)
        {
            var dept = await Repo.GetById<Department>(dto.DepartmentId.Value);
            if (dept is null)
                return BadRequest(new { Message = "Department not found." });
        }

        existing.Username = string.IsNullOrWhiteSpace(dto.Username) ? existing.Username : dto.Username;
        existing.FullName = dto.FullName ?? existing.FullName;
        existing.Email = dto.Email ?? existing.Email;
        existing.PhoneNumber = dto.PhoneNumber ?? existing.PhoneNumber;
        existing.DepartmentId = dto.DepartmentId;
        existing.IsActive = dto.IsActive ?? existing.IsActive;
        existing.IsAdmin = dto.IsAdmin ?? existing.IsAdmin;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            existing.PasswordHash = HashPassword(dto.Password);

        SetId(existing, id);
        SetUpdatedTimestamp(existing);
        var saved = await Repo.Update(existing);
        return Ok(saved);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

public class UserDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
    public string? Password { get; set; }
}
