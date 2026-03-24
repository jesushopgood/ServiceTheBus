using MediatR;

namespace OrderService.Application.Features.Ping;

public sealed record GetPingQuery(string Name) : IRequest<GetPingResponse>;

public sealed record GetPingResponse(string Service, string Message, DateTime UtcTimestamp);
