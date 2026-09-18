using FluentValidation;
using SmartSolarMicrogrid.API.DTOs.Transfers;

namespace SmartSolarMicrogrid.API.Validators;

public class VerifyTransferRequestValidator : AbstractValidator<VerifyTransferRequest>
{
    public VerifyTransferRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Payload) || (!string.IsNullOrEmpty(x.ReservationId) && !string.IsNullOrEmpty(x.BackupCode)))
            .WithMessage("Either a valid QR payload or both ReservationId and BackupCode must be provided.");
    }
}

public class FinalizeTransferRequestValidator : AbstractValidator<FinalizeTransferRequest>
{
    public FinalizeTransferRequestValidator()
    {
        RuleFor(x => x.MeterStartKwh)
            .GreaterThanOrEqualTo(0).WithMessage("Meter start kWh cannot be negative.");

        RuleFor(x => x.MeterEndKwh)
            .GreaterThan(x => x.MeterStartKwh)
            .WithMessage("Meter end reading must be strictly greater than meter start reading.");
    }
}
