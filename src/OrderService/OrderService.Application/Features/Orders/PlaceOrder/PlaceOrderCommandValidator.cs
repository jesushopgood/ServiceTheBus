using FluentValidation;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.PlaceOrder;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.TotalItems)
            .GreaterThan(0)
            .WithMessage("totalItems must be greater than zero.");

        RuleFor(x => x)
            .CustomAsync(async (request, context, cancellationToken) =>
            {
                var products = await productRepository.GetAllAsync(cancellationToken);

                if (products.Count == 0)
                {
                    context.AddFailure("No products found to create an order.");
                    return;
                }

                if (request.TotalItems > products.Count)
                {
                    context.AddFailure(nameof(request.TotalItems),
                        $"totalItems cannot be greater than available products ({products.Count}).");
                }
            });
    }
}
