using FluentValidation;
using FMS.Application.Features.Vehicles.Commands;

namespace FMS.Application.Common.Validators;

public class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Request.VehicleNumber)
            .NotEmpty().WithMessage("Vehicle number is required")
            .MaximumLength(50).WithMessage("Vehicle number must not exceed 50 characters");

        RuleFor(x => x.Request.Type)
            .MaximumLength(50).WithMessage("Vehicle type must not exceed 50 characters");

        RuleFor(x => x.Request.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage("Year must be between 1900 and next year");
    }
}
