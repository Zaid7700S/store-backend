using System.Text.Json.Serialization;

namespace store.Models
{
    public class Cart_Items
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int CartId { get; set; }
        public int Quantity { get; set; }
        [JsonIgnore]
        public Carts? Cart { get; set; }
        public Products? Product { get; set; }
    }
}
