using FluentValidation;
using SmartSolarMicrogrid.API.DTOs.Reservations;

namespace SmartSolarMicrogrid.API.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.NodeId)
            .NotEmpty().WithMessage("NodeId is required.");

        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("SlotId is required.");

        RuleFor(x => x.TradeType)
            .Must(t => t == "Export" || t == "Import")
            .WithMessage("TradeType must be either 'Export' or 'Import'.");

        RuleFor(x => x.RequestedKwh)
            .GreaterThan(0).WithMessage("Requested energy (kWh) must be greater than zero.");
    }
}
