using System.Text.Json.Serialization;

namespace store.Models
{
    public class Carts
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [JsonIgnore]
        public Users User { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<Cart_Items> Cart_Items { get; set; } = new (); 
    }
}
