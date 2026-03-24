using common.Messaging;
using MediatR;

namespace OrderService.Application.Features.Orders.ProcessSupplierQuote;

public sealed record ProcessSupplierQuoteCommand(SupplierQuoteMessage Quote) : IRequest;
