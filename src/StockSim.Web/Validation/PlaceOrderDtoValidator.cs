using FluentValidation;
using StockSim.Web.Controllers;

namespace StockSim.Web.Validation;

public sealed class PlaceOrderDtoValidator : AbstractValidator<TradingController.PlaceOrderDto>
{
    public PlaceOrderDtoValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(15)
            .Matches("^[A-Za-z0-9][A-Za-z0-9.\\-]{0,14}$");

        RuleFor(x => x.Quantity)
            .GreaterThan(0m);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Side)
            .IsInEnum();

        When(x => x.Type == Domain.Orders.OrderType.Limit, () =>
        {
            RuleFor(x => x.LimitPrice)
                .NotNull()
                .GreaterThanOrEqualTo(0m);
        });

        When(x => x.Type == Domain.Orders.OrderType.Market, () =>
        {
            RuleFor(x => x.LimitPrice)
                .Must(p => p == null || p == 0m)
                .WithMessage("Market orders must not specify LimitPrice.");
        });
    }
}
