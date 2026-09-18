using FluentValidation;
using SmartSolarMicrogrid.API.DTOs.Nodes;
using SmartSolarMicrogrid.API.DTOs.Slots;

namespace SmartSolarMicrogrid.API.Validators;

public class CreateNodeValidator : AbstractValidator<CreateNodeDto>
{
    public CreateNodeValidator()
    {
        RuleFor(x => x.NodeCode)
            .NotEmpty().WithMessage("Node code is required.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("Node code must be alphanumeric and hyphens only.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Node name is required.");

        RuleFor(x => x.BuyPricePerKwh)
            .GreaterThan(0).WithMessage("Buy price per kWh must be greater than zero.");

        RuleFor(x => x.SellPricePerKwh)
            .GreaterThan(0).WithMessage("Sell price per kWh must be greater than zero.");

        RuleFor(x => x.CapacityBays)
            .GreaterThan(0).WithMessage("Capacity bays must be at least 1.");

        RuleFor(x => x.MaxKwhPerReservation)
            .GreaterThan(0).WithMessage("Max kWh per reservation must be greater than zero.");
    }
}

public class UpdateSlotValidator : AbstractValidator<UpdateSlotDto>
{
    public UpdateSlotValidator()
    {
        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(0).When(x => x.Capacity.HasValue)
            .WithMessage("Capacity cannot be negative.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrEmpty(s) || s == "Available" || s == "Blocked")
            .WithMessage("Status must be either 'Available' or 'Blocked'.");
    }
}
