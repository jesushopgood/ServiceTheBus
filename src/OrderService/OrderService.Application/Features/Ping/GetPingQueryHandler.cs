using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderService.Application.Features.Ping;

public sealed class GetPingQueryHandler(ILogger<GetPingQueryHandler> logger)
    : IRequestHandler<GetPingQuery, GetPingResponse>
{
    public Task<GetPingResponse> Handle(GetPingQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ping request for {Name}", request.Name);

        var normalizedName = string.IsNullOrWhiteSpace(request.Name) ? "Anonymous" : request.Name.Trim();
        var response = new GetPingResponse(
            "OrderService",
            $"Hello {normalizedName}, OrderService clean architecture is active.",
            DateTime.UtcNow);

        return Task.FromResult(response);
    }
}

