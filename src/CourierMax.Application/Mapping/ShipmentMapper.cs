using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using CourierMax.Domain.Entities;

namespace CourierMax.Application.Mapping;

/// <summary>
/// Traduce el agregado de dominio a su representacion de salida, enriqueciendola
/// con datos calculados (fecha limite y estado de atraso) que dependen del SLA.
/// </summary>
public static class ShipmentMapper
{
    public static ShipmentResponse ToResponse(Shipment shipment, ISlaService sla, DateTime asOf)
    {
        var history = shipment.History
            .Select(h => new StatusChangeResponse(h.From, h.To, h.ChangedAt, h.ChangedBy, h.Reason))
            .ToList();

        return new ShipmentResponse(
            shipment.Id,
            shipment.TrackingCode,
            shipment.Status,
            shipment.Service,
            shipment.Origin,
            shipment.Destination,
            shipment.Cost,
            shipment.CreatedAt,
            sla.GetDeadline(shipment.CreatedAt, shipment.Service),
            sla.IsDelayed(shipment, asOf),
            new ContactDto(shipment.Sender.Name, shipment.Sender.Phone, shipment.Sender.Address),
            new ContactDto(shipment.Recipient.Name, shipment.Recipient.Phone, shipment.Recipient.Address),
            new PackageDto(
                shipment.Package.WeightKg,
                shipment.Package.LengthCm,
                shipment.Package.WidthCm,
                shipment.Package.HeightCm,
                shipment.Package.Type),
            shipment.AssignedDriverId,
            shipment.AssignedVehicleId,
            history);
    }
}
