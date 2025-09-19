using Application.Common;
using Application.Contracts.Infrastructure;
using Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CartController> _logger;
    private readonly TimeSpan _cartExpiry = TimeSpan.FromHours(24);

    public CartController(ICacheService cacheService, ILogger<CartController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("{cartId}")]
    public async Task<ActionResult<CartVm>> GetCart(string cartId)
    {
        var cacheKey = CacheKeys.CartItems(cartId);
        var cartItems = await _cacheService.GetAsync<List<CartItemVm>>(cacheKey) ?? new List<CartItemVm>();

        var cart = new CartVm
        {
            CartId = cartId,
            Items = cartItems
        };

        return Ok(cart);
    }

    [HttpPost("{cartId}/items")]
    public async Task<ActionResult<AddToCartResponseVm>> AddToCart(string cartId, [FromBody] AddToCartRequest request)
    {
        var cacheKey = CacheKeys.CartItems(cartId);
        var cartItems = await _cacheService.GetAsync<List<CartItemVm>>(cacheKey) ?? new List<CartItemVm>();

        var existingItem = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cartItems.Add(new CartItemVm
            {
                ProductId = request.ProductId,
                ProductName = request.ProductName,
                Price = request.Price,
                Quantity = request.Quantity
            });
        }

        await _cacheService.SetAsync(cacheKey, cartItems, _cartExpiry);

        return Ok(new AddToCartResponseVm
        {
            Success = true,
            CartItemCount = cartItems.Sum(x => x.Quantity),
            CartTotal = cartItems.Sum(x => x.SubTotal),
            Message = "Item added to cart successfully"
        });
    }

    [HttpPut("{cartId}/items/{productId}")]
    public async Task<ActionResult<CartVm>> UpdateCartItem(string cartId, int productId, [FromBody] UpdateQuantityRequest request)
    {
        var cacheKey = CacheKeys.CartItems(cartId);
        var cartItems = await _cacheService.GetAsync<List<CartItemVm>>(cacheKey) ?? new List<CartItemVm>();

        var item = cartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item == null)
        {
            return NotFound($"Product {productId} not found in cart");
        }

        if (request.Quantity <= 0)
        {
            cartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        await _cacheService.SetAsync(cacheKey, cartItems, _cartExpiry);

        return Ok(new CartVm
        {
            CartId = cartId,
            Items = cartItems
        });
    }

    [HttpDelete("{cartId}/items/{productId}")]
    public async Task<IActionResult> RemoveFromCart(string cartId, int productId)
    {
        var cacheKey = CacheKeys.CartItems(cartId);
        var cartItems = await _cacheService.GetAsync<List<CartItemVm>>(cacheKey) ?? new List<CartItemVm>();

        cartItems.RemoveAll(x => x.ProductId == productId);

        await _cacheService.SetAsync(cacheKey, cartItems, _cartExpiry);

        return NoContent();
    }

    [HttpDelete("{cartId}")]
    public async Task<IActionResult> ClearCart(string cartId)
    {
        var cacheKey = CacheKeys.CartItems(cartId);
        await _cacheService.RemoveAsync(cacheKey);

        return NoContent();
    }
}

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class UpdateQuantityRequest
{
    public int Quantity { get; set; }
}