using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;

namespace CourierMax.Application.Services;

/// <summary>
/// Construye el reporte de eficiencia por conductor (RF-06): volumenes por estado,
/// tiempo promedio de entrega, cumplimiento de SLA y peso transportado.
/// </summary>
public sealed class MetricsService : IMetricsService
{
    private readonly IShipmentRepository _shipments;
    private readonly IDriverRepository _drivers;
    private readonly ISlaService _sla;

    public MetricsService(IShipmentRepository shipments, IDriverRepository drivers, ISlaService sla)
    {
        _shipments = shipments;
        _drivers = drivers;
        _sla = sla;
    }

    public DriverMetricsResponse GetForDriver(int driverId)
    {
        var driver = _drivers.GetById(driverId)
            ?? throw new NotFoundException($"No existe un conductor con id {driverId}.");
        return Build(driver);
    }

    public IReadOnlyList<DriverMetricsResponse> GetAll()
        => _drivers.GetAll().Select(Build).ToList();

    private DriverMetricsResponse Build(Driver driver)
    {
        var assigned = _shipments.GetAll()
            .Where(s => s.AssignedDriverId == driver.Id)
            .ToList();

        var delivered = assigned.Where(s => s.Status == ShipmentStatus.Entregado).ToList();

        var averageDeliveryDays = delivered.Count == 0
            ? 0d
            : delivered
                .Where(s => s.AssignedAt.HasValue && s.DeliveredAt.HasValue)
                .Select(s => (s.DeliveredAt!.Value - s.AssignedAt!.Value).TotalDays)
                .DefaultIfEmpty(0d)
                .Average();

        var onTimeCount = delivered.Count(IsDeliveredOnTime);
        var onTimePercentage = delivered.Count == 0
            ? 0d
            : Math.Round(onTimeCount * 100d / delivered.Count, 2);

        // Peso efectivamente transportado: envios que estuvieron o estan en la via
        // (en transito o ya entregados).
        var transportedWeight = assigned
            .Where(s => s.Status is ShipmentStatus.EnTransito or ShipmentStatus.Entregado)
            .Sum(s => s.Package.WeightKg);

        return new DriverMetricsResponse(
            driver.Id,
            driver.Name,
            assigned.Count,
            delivered.Count,
            assigned.Count(s => s.Status == ShipmentStatus.Cancelado),
            assigned.Count(s => s.Status == ShipmentStatus.EnTransito),
            Math.Round(averageDeliveryDays, 2),
            onTimePercentage,
            transportedWeight);
    }

    private bool IsDeliveredOnTime(Shipment shipment)
    {
        if (!shipment.DeliveredAt.HasValue)
            return false;

        var deadline = _sla.GetDeadline(shipment.CreatedAt, shipment.Service);
        return shipment.DeliveredAt.Value.Date <= deadline.Date;
    }
}
