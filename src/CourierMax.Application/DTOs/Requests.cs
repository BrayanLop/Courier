using CourierMax.Domain.Enums;

namespace CourierMax.Application.DTOs;

public sealed record ContactDto(string Name, string Phone, string Address);

public sealed record PackageDto(
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    PackageType Type);

/// <summary>Datos de entrada para registrar un envio (RF-01).</summary>
public sealed record CreateShipmentRequest(
    ContactDto Sender,
    ContactDto Recipient,
    PackageDto Package,
    ServiceType Service,
    string Origin,
    string Destination,
    string? CreatedBy);

/// <summary>
/// Asignacion de un envio (RF-03). Si DriverId viene nulo, el sistema elige el
/// vehiculo con capacidad disponible y menor carga actual (RN-01).
/// </summary>
public sealed record AssignShipmentRequest(int? DriverId, string ChangedBy);

public sealed record StatusChangeRequest(string ChangedBy);

public sealed record CancelShipmentRequest(string Reason, string ChangedBy);
