using common.Messaging;
using MediatR;

namespace SupplierENG.Application.Features.Orders.BuildQuote;

public sealed record BuildQuoteCommand(OrderPlacedMessage Order, string SupplierCode) : IRequest<SupplierQuoteMessage>;
