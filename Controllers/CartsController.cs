using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store.Data;
using store.Dto;
using store.Models;
using System.Security.Claims;

namespace store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController(StoreDbContext context) : ControllerBase
    {
        private readonly StoreDbContext _context = context;

        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<ActionResult<List<Carts>>> GetAllCarts()
        {
            var carts = await _context.Carts
                .Include(x=> x.Cart_Items)
                .ToListAsync();
            return Ok(carts);
        }

        
        [HttpGet("cart/{userId}")]
        [Authorize(Roles ="Admin")]

        public async Task<ActionResult<Carts>> GetCartByUserId(int userId)
        {
            var cart = await _context.Carts
                .Include(x => x.Cart_Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (cart is null)
                return NotFound();
            return Ok(cart);

        }

        [Authorize]
        [HttpGet("cart")] 
        public async Task<ActionResult<Carts>> GetUserCart()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var cart = await _context.Carts
                .Include(x => x.Cart_Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);



            if (cart is null)
                return NotFound();
            return Ok(cart);
            
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Carts>> GetCartById(int id)
        {
            var cart = await _context.Carts
                .Include(x => x.Cart_Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cart is null)
                return NotFound();
            return Ok(cart);

        }

        [HttpPost]
        [Authorize]

        public async Task<ActionResult<Carts>> CreateCart(CreateCartDto newCart)
        {
            if (newCart is null)
                return BadRequest();

            var cart = new Carts
            {
                UserId = newCart.UserId,
                TotalAmount = 0
            };

            await _context.Carts.AddAsync(cart);

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCartById), new { id = cart.Id }, cart);
    
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCart(int id)
        {
            var cart = await _context.Carts.FindAsync(id);

            if (cart is null)
                return NotFound();

            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();

            return NoContent();

        }

        [HttpPost("{id}/items")]
        [Authorize]
        public async Task<IActionResult> AddCartItem(int id,AddCart_ItemsDto newItem)
        {
            if (newItem is null)
                return BadRequest();

            var cart = await _context.Carts
                        .Include(x=> x.Cart_Items)
                        .FirstOrDefaultAsync(x => x.Id == id);

            if (cart is null)
                return NotFound();

            var product = await _context.Products.FindAsync(newItem.ProductId);

            if (product is null)
                return NotFound();

            var existingItem = cart.Cart_Items.FirstOrDefault(x => x.ProductId == newItem.ProductId);

            if(existingItem != null)
            {
                existingItem.Quantity += newItem.Quantity;
            }
            else
            {
                var Item = new Cart_Items
                {
                    ProductId = newItem.ProductId,
                    Quantity = newItem.Quantity
                };

                cart.Cart_Items.Add(Item);
            }

            cart.TotalAmount += (product.Price * newItem.Quantity);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}/items/{productId}")]
        [Authorize]
        public async Task<IActionResult> DeleteCartItem(int id,int productId)
        {
            var cart = await _context.Carts
                        .Include(x => x.Cart_Items)
                        .ThenInclude(x => x.Product)
                        .FirstOrDefaultAsync(x => x.Id == id);

            if (cart is null)
                return NotFound();


            var item = cart.Cart_Items.FirstOrDefault(x => x.ProductId == productId);

            if (item is null)
                return NotFound();

            cart.TotalAmount -= item.Product!.Price * item.Quantity;

            cart.Cart_Items.Remove(item);

            await _context.SaveChangesAsync();

            return NoContent();

        }

        [HttpPut("{id}/items/{productId}")]
        [Authorize]
        public async Task<IActionResult> UpdateCartItem (int id,int productId ,UpdateCartItemDto updated)
        {
            if (updated.Quantity < 1)
                return BadRequest();

            var cart = await _context.Carts
                       .Include(x => x.Cart_Items)
                       .ThenInclude(x => x.Product)
                       .FirstOrDefaultAsync(x => x.Id == id);

            if (cart is null)
                return NotFound();

            var item = cart.Cart_Items.FirstOrDefault(x => x.ProductId == productId);

            if (item is null)
                return NotFound();

            item.Quantity = updated.Quantity;

            cart.TotalAmount = cart.Cart_Items.Sum(i => i.Quantity * i.Product!.Price);

            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
