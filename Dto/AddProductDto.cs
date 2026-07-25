namespace store.Dto
{
    public class AddProductDto
    {
        public string? Name { get; set; } = string.Empty;
        public int Quantity { get; set; } 
        public string? Description { get; set; } = string.Empty;
        public float Rating { get; set; } 
        public Decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
