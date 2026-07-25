using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using store.Data;
using store.Dto;
using store.Models;

namespace store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(StoreDbContext context,IConfiguration configuration) : ControllerBase
    {
        private readonly StoreDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;


        [HttpGet]
        public async Task<ActionResult<List<Products>>> GetAllProducts([FromQuery] int? limit)
        {
            var query = _context.Products.AsQueryable();

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var products = await query.ToListAsync();

            return Ok(products);
        }

        [HttpGet("{id}")] 
        public async Task<ActionResult<Products>> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null)
                return NotFound();
            return Ok(product);
        }
        
        [HttpPost]
        [Authorize(Roles="Admin")]
        public async Task<ActionResult<Products>> AddProduct(AddProductDto newProduct)
        {
            if (newProduct is null)
                return BadRequest();

            var product = new Products
            {
                Name = newProduct.Name,
                Quantity = newProduct.Quantity,
                Description = newProduct.Description,
                Rating = newProduct.Rating,
                Price = newProduct.Price,
                Category = newProduct.Category
            };

            await _context.Products.AddAsync(product);

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProductById),new { id = product.Id },product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id,UpdateProductDto updated)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound();

            product.Name = updated.Name;
            product.Quantity = updated.Quantity;
            product.Description = updated.Description;
            product.Rating = updated.Rating;
            product.Price = updated.Price;
            product.Category = updated.Category;

            await _context.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null)
                return NotFound();

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("search")]

        public async Task<IActionResult> SearchProducts(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return Ok(await _context.Products.Take(10).ToListAsync());
            }

            var results = await _context.Products
                            .Where(x => x.Name!.Contains(s.ToLower()))
                            .Take(10)
                            .ToListAsync();

            return Ok(results);
        }



    [HttpPost("{id}/upload-image")]
    [Authorize(Roles = "Admin")] // Security constraint
    public async Task<IActionResult> UploadProductImage(int id, IFormFile image)
    {
        if (image is null || image.Length == 0)
            return BadRequest("No image uploaded.");

        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound("Product not found.");

        // Generate a clean, unique file name
        var fileExtension = Path.GetExtension(image.FileName);
        var uniqueFileName = $"products/{Guid.NewGuid()}{fileExtension}";

        // Configure R2 connection
        var accountId = _configuration["R2Settings:AccountId"];
        using var s3Client = new AmazonS3Client(
            _configuration["R2Settings:AccessKey"],
            _configuration["R2Settings:SecretKey"],
            new AmazonS3Config { ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com" }
        );

        // Upload to Cloudflare R2
        using var stream = image.OpenReadStream();
        var putRequest = new PutObjectRequest
        {
            BucketName = _configuration["R2Settings:BucketName"],
            Key = uniqueFileName,
            InputStream = stream,
            ContentType = image.ContentType,
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest);

        // Save public URL to DB
        var publicDomain = _configuration["R2Settings:PublicDomain"];
        product.ImageUrl = $"{publicDomain}/{uniqueFileName}";
        await _context.SaveChangesAsync();

        return Ok(new { imageUrl = product.ImageUrl });
    }

        [HttpGet("category/{categoryName}")]
        public async Task<ActionResult<IEnumerable<Products>>> GetProductsByCategory(string categoryName)
        {
            // Fetch all products first
            var allProducts = await _context.Products.ToListAsync();

            // Filter in memory by splitting the comma-separated string
            // This safely ensures that searching for "Men" doesn't accidentally match "Women"
            var filteredProducts = allProducts.Where(p =>
                !string.IsNullOrEmpty(p.Category) &&
                p.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                          .Any(c => c.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            return Ok(filteredProducts);
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllCategories()
        {
            var allProducts = await _context.Products.ToListAsync();

            // Extract all unique tags from the comma-separated strings
            var categories = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .SelectMany(p => p.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }



    }
}
