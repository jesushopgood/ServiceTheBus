using MediatR;
using Microsoft.AspNetCore.Mvc;
using SupplierENG.Application.Features.Ping;

namespace SupplierENG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PingController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetPingResponse>> Get([FromQuery] string? name, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetPingQuery(name ?? "World"), cancellationToken);
        return Ok(response);
    }
}

