using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;

namespace CourierMax.Domain.Entities;

/// <summary>
/// Raiz del agregado de envio. Concentra el estado y las reglas de transicion
/// (RF-02, RN-03) de forma que el ciclo de vida no pueda quedar en un estado
/// invalido desde fuera del dominio.
/// </summary>
public sealed class Shipment
{
    private const int MinCancellationReasonLength = 5;

    private readonly List<StatusChange> _history = new();

    public Guid Id { get; }
    public string TrackingCode { get; }
    public ContactInfo Sender { get; }
    public ContactInfo Recipient { get; }
    public PackageDetails Package { get; }
    public ServiceType Service { get; }
    public string Origin { get; }
    public string Destination { get; }
    public decimal Cost { get; }
    public DateTime CreatedAt { get; }

    public ShipmentStatus Status { get; private set; }
    public int? AssignedDriverId { get; private set; }
    public int? AssignedVehicleId { get; private set; }

    public IReadOnlyList<StatusChange> History => _history.AsReadOnly();

    public Shipment(
        Guid id,
        string trackingCode,
        ContactInfo sender,
        ContactInfo recipient,
        PackageDetails package,
        ServiceType service,
        string origin,
        string destination,
        decimal cost,
        DateTime createdAt,
        string createdBy)
    {
        Id = id;
        TrackingCode = trackingCode;
        Sender = sender;
        Recipient = recipient;
        Package = package;
        Service = service;
        Origin = origin;
        Destination = destination;
        Cost = cost;
        CreatedAt = createdAt;
        Status = ShipmentStatus.Creado;

        _history.Add(new StatusChange(null, ShipmentStatus.Creado, createdAt, createdBy, "Creacion del envio"));
    }

    /// <summary>Indica si el envio esta ocupando capacidad de un vehiculo en este momento.</summary>
    public bool OccupiesVehicleCapacity =>
        Status is ShipmentStatus.Asignado or ShipmentStatus.EnTransito;

    /// <summary>Momento en que el envio paso a ASIGNADO, si aplica.</summary>
    public DateTime? AssignedAt =>
        _history.FirstOrDefault(h => h.To == ShipmentStatus.Asignado)?.ChangedAt;

    /// <summary>Momento en que el envio paso a ENTREGADO, si aplica.</summary>
    public DateTime? DeliveredAt =>
        _history.FirstOrDefault(h => h.To == ShipmentStatus.Entregado)?.ChangedAt;

    public void AssignTo(int driverId, int vehicleId, string changedBy, DateTime when)
    {
        EnsureTransitionAllowed(ShipmentStatus.Asignado);

        AssignedDriverId = driverId;
        AssignedVehicleId = vehicleId;
        ChangeStatus(ShipmentStatus.Asignado, changedBy, when, $"Asignado al conductor {driverId}");
    }

    public void MarkInTransit(string changedBy, DateTime when)
    {
        EnsureTransitionAllowed(ShipmentStatus.EnTransito);
        ChangeStatus(ShipmentStatus.EnTransito, changedBy, when, "Envio en transito");
    }

    public void MarkDelivered(string changedBy, DateTime when)
    {
        EnsureTransitionAllowed(ShipmentStatus.Entregado);
        ChangeStatus(ShipmentStatus.Entregado, changedBy, when, "Envio entregado");
    }

    public void Cancel(string reason, string changedBy, DateTime when)
    {
        if (Status == ShipmentStatus.Entregado)
            throw new ConflictException("No se puede cancelar un envio que ya fue entregado.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < MinCancellationReasonLength)
            throw new BusinessRuleException(
                $"El motivo de cancelacion es obligatorio y debe tener al menos {MinCancellationReasonLength} caracteres.");

        // Al cancelar se libera la capacidad del vehiculo (RN-03): se logra
        // porque OccupiesVehicleCapacity deja de ser verdadero en este estado.
        ChangeStatus(ShipmentStatus.Cancelado, changedBy, when, reason.Trim());
    }

    private void EnsureTransitionAllowed(ShipmentStatus target)
    {
        var allowed = Status switch
        {
            ShipmentStatus.Creado => target == ShipmentStatus.Asignado,
            ShipmentStatus.Asignado => target == ShipmentStatus.EnTransito,
            ShipmentStatus.EnTransito => target == ShipmentStatus.Entregado,
            _ => false
        };

        if (!allowed)
            throw new ConflictException(
                $"Transicion invalida: no se puede pasar de {Status} a {target}.");
    }

    private void ChangeStatus(ShipmentStatus target, string changedBy, DateTime when, string? reason)
    {
        var previous = Status;
        Status = target;
        _history.Add(new StatusChange(previous, target, when, changedBy, reason));
    }
}
