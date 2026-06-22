using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs;
using FluentValidation;

namespace CourierMax.Application.Validation;

public sealed class ContactDtoValidator : AbstractValidator<ContactDto>
{
    // Telefono colombiano: 10 digitos que inician con 3 o 6 (RN-04).
    private const string ColombianPhonePattern = @"^[36]\d{9}$";

    public ContactDtoValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(c => c.Address)
            .NotEmpty().WithMessage("La direccion es obligatoria.");

        RuleFor(c => c.Phone)
            .NotEmpty().WithMessage("El telefono es obligatorio.")
            .Matches(ColombianPhonePattern)
            .WithMessage("El telefono debe tener 10 digitos e iniciar con 3 o 6.");
    }
}

public sealed class PackageDtoValidator : AbstractValidator<PackageDto>
{
    public PackageDtoValidator()
    {
        RuleFor(p => p.WeightKg)
            .InclusiveBetween(0.1m, 100m)
            .WithMessage("El peso debe estar entre 0.1 kg y 100 kg.");

        RuleFor(p => p.LengthCm)
            .InclusiveBetween(1m, 200m).WithMessage("El largo debe estar entre 1 cm y 200 cm.");
        RuleFor(p => p.WidthCm)
            .InclusiveBetween(1m, 200m).WithMessage("El ancho debe estar entre 1 cm y 200 cm.");
        RuleFor(p => p.HeightCm)
            .InclusiveBetween(1m, 200m).WithMessage("El alto debe estar entre 1 cm y 200 cm.");

        RuleFor(p => p.Type)
            .IsInEnum().WithMessage("El tipo de paquete no es valido.");
    }
}

public sealed class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator(ICityCatalog cities)
    {
        RuleFor(r => r.Sender).NotNull().SetValidator(new ContactDtoValidator());
        RuleFor(r => r.Recipient).NotNull().SetValidator(new ContactDtoValidator());
        RuleFor(r => r.Package).NotNull().SetValidator(new PackageDtoValidator());

        RuleFor(r => r.Service)
            .IsInEnum().WithMessage("El tipo de servicio no es valido.");

        RuleFor(r => r.Origin)
            .NotEmpty().WithMessage("La ciudad de origen es obligatoria.")
            .Must(cities.IsValid).WithMessage(c => $"La ciudad de origen '{c.Origin}' no es valida.");

        RuleFor(r => r.Destination)
            .NotEmpty().WithMessage("La ciudad de destino es obligatoria.")
            .Must(cities.IsValid).WithMessage(c => $"La ciudad de destino '{c.Destination}' no es valida.");

        RuleFor(r => r)
            .Must(r => !string.Equals(r.Origin, r.Destination, StringComparison.OrdinalIgnoreCase))
            .WithMessage("El origen y el destino no pueden ser la misma ciudad.")
            .WithName("Route");
    }
}

public sealed class AssignShipmentRequestValidator : AbstractValidator<AssignShipmentRequest>
{
    public AssignShipmentRequestValidator()
    {
        RuleFor(r => r.ChangedBy)
            .NotEmpty().WithMessage("El identificador de quien realiza el cambio es obligatorio.");

        RuleFor(r => r.DriverId)
            .GreaterThan(0).When(r => r.DriverId.HasValue)
            .WithMessage("El identificador del conductor debe ser positivo.");
    }
}

public sealed class StatusChangeRequestValidator : AbstractValidator<StatusChangeRequest>
{
    public StatusChangeRequestValidator()
    {
        RuleFor(r => r.ChangedBy)
            .NotEmpty().WithMessage("El identificador de quien realiza el cambio es obligatorio.");
    }
}

public sealed class CancelShipmentRequestValidator : AbstractValidator<CancelShipmentRequest>
{
    public CancelShipmentRequestValidator()
    {
        RuleFor(r => r.ChangedBy)
            .NotEmpty().WithMessage("El identificador de quien realiza el cambio es obligatorio.");

        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage("El motivo de cancelacion es obligatorio.")
            .MinimumLength(5).WithMessage("El motivo de cancelacion debe tener al menos 5 caracteres.");
    }
}
