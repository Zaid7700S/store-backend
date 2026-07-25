using store.Models;

namespace store.Dto
{
    public class UserDto
    {
        public int id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? userName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public List<Carts> Carts { get; set; } = new();
    }
}
