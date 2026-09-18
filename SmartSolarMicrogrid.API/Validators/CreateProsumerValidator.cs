using FluentValidation;
using SmartSolarMicrogrid.API.DTOs.Prosumer;

namespace SmartSolarMicrogrid.API.Validators;

public class CreateProsumerValidator : AbstractValidator<CreateProsumerDto>
{
    public CreateProsumerValidator()
    {
        RuleFor(x => x.NIC)
            .NotEmpty().WithMessage("NIC is required.")
            .Matches(@"^(\d{9}[VvXx]|\d{12})$")
            .WithMessage("NIC must be 9 digits followed by V/X, or 12 digits (e.g. 199012345678 or 901234567V).");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone must be a 10-digit number.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}
