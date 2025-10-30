using FluentValidation;
using StockSim.Web.Controllers;

namespace StockSim.Web.Validation;

public sealed class CancelOrderDtoValidator : AbstractValidator<TradingController.CancelOrderDto>
{
    public CancelOrderDtoValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}
