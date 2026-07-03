namespace R_FACTORY_BE.Models.Models;

public partial class RefreshToken
{
    public User? User { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive => !IsExpired && !IsRevoked;
}
