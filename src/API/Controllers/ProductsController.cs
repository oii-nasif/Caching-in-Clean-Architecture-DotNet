using Application.Common;
using Application.Contracts.Infrastructure;
using Application.Features.Products.Queries;
using Application.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ICacheService cacheService, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CacheResponseVm<ProductDetailsVm>>> GetProduct(int id)
    {
        var query = new GetProductDetailsQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<List<ProductVm>>> GetProductsByCategory(string category)
    {
        var query = new GetProductsByCategoryQuery(category);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpDelete("cache/{id}")]
    public async Task<IActionResult> RemoveFromCache(int id)
    {
        var cacheKey = CacheKeys.ProductDetails(id);
        await _cacheService.RemoveAsync(cacheKey);
        return NoContent();
    }

    [HttpDelete("cache/category/{category}")]
    public async Task<IActionResult> RemoveCategoryFromCache(string category)
    {
        await _cacheService.RemoveByPatternAsync($"products:category:{category}");
        return NoContent();
    }
}