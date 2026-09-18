using FluentValidation;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Auth;

namespace SmartSolarMicrogrid.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly string[] AllowedRoles =
        [RoleConstants.Backoffice, RoleConstants.GridOperator];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
