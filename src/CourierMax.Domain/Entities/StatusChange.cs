using CourierMax.Domain.Enums;

namespace CourierMax.Domain.Entities;

/// <summary>
/// Entrada del historial de estados de un envio (RF-02). Es inmutable: una vez
/// registrado el cambio no se modifica.
/// </summary>
public sealed class StatusChange
{
    public ShipmentStatus? From { get; }
    public ShipmentStatus To { get; }
    public DateTime ChangedAt { get; }
    public string? Reason { get; }
    public string ChangedBy { get; }

    public StatusChange(ShipmentStatus? from, ShipmentStatus to, DateTime changedAt, string changedBy, string? reason)
    {
        From = from;
        To = to;
        ChangedAt = changedAt;
        ChangedBy = changedBy;
        Reason = reason;
    }
}
