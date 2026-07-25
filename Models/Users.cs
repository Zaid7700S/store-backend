using store.Dto;
using System.ComponentModel.DataAnnotations;

namespace store.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? userName { get; set; }
        public string? password { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string? ResetOtp { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<Carts> Carts { get; set; } = new(); 

    }
}
