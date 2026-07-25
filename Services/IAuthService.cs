using store.Dto;
using store.Models;

namespace store.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(Users user);
        string GenerateRefreshToken();
        string GeneratePasswordResetToken(Users user);
    }
}
