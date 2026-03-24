using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.Orders.PlaceOrder;
using OrderService.Application.Features.Orders.Models;
using OrderService.Application.Features.Products.GetById;
using OrderService.Application.Features.Products.GetProducts;
using OrderService.Application.Features.Products.Models;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrderController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<OrderController> _logger;
    public OrderController(ISender sender, ILogger<OrderController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost("place-order")]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromQuery] int totalItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"OrderController::PlaceOrder TotalItems = {totalItems}");
        var order = await _sender.Send(new PlaceOrderCommand(totalItems), cancellationToken);
        return Ok(order);
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Get(CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new GetProductsQuery(), cancellationToken);
        return Ok(products);
    }

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}
