namespace store.Models
{
    public class Products
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public float Rating { get; set; }
        public Decimal Price { get; set; }
        public string Category { get; set; } = "Uncategorized";
        public string? ImageUrl { get; set; }
    }
}
