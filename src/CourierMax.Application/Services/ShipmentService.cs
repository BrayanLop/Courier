using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs;
using CourierMax.Application.Mapping;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CourierMax.Application.Services;

/// <summary>
/// Orquesta el ciclo de vida del envio (RF-01, RF-02, RF-03, RF-05). Coordina
/// repositorios y servicios de dominio, pero delega las reglas de transicion al
/// propio agregado y las de capacidad al servicio de asignacion.
/// </summary>
public sealed class ShipmentService : IShipmentService
{
    private const int MaxTrackingCodeAttempts = 10;
    private const string DefaultActor = "system";

    private readonly IShipmentRepository _shipments;
    private readonly ITariffCalculator _tariff;
    private readonly ITrackingCodeGenerator _trackingCodes;
    private readonly IAssignmentService _assignment;
    private readonly ISlaService _sla;
    private readonly IClock _clock;
    private readonly ILogger<ShipmentService> _logger;

    public ShipmentService(
        IShipmentRepository shipments,
        ITariffCalculator tariff,
        ITrackingCodeGenerator trackingCodes,
        IAssignmentService assignment,
        ISlaService sla,
        IClock clock,
        ILogger<ShipmentService> logger)
    {
        _shipments = shipments;
        _tariff = tariff;
        _trackingCodes = trackingCodes;
        _assignment = assignment;
        _sla = sla;
        _clock = clock;
        _logger = logger;
    }

    public ShipmentResponse Create(CreateShipmentRequest request)
    {
        var sender = new ContactInfo(request.Sender.Name, request.Sender.Phone, request.Sender.Address);
        var recipient = new ContactInfo(request.Recipient.Name, request.Recipient.Phone, request.Recipient.Address);
        var package = new PackageDetails(
            request.Package.WeightKg,
            request.Package.LengthCm,
            request.Package.WidthCm,
            request.Package.HeightCm,
            request.Package.Type);

        var cost = _tariff.Calculate(package, request.Service, request.Origin, request.Destination).Total;
        var trackingCode = GenerateUniqueTrackingCode();
        var actor = string.IsNullOrWhiteSpace(request.CreatedBy) ? DefaultActor : request.CreatedBy!;

        var shipment = new Shipment(
            Guid.NewGuid(),
            trackingCode,
            sender,
            recipient,
            package,
            request.Service,
            request.Origin,
            request.Destination,
            cost,
            _clock.Now,
            actor);

        _shipments.Add(shipment);
        _logger.LogInformation("Envio {TrackingCode} creado con costo {Cost}.", trackingCode, cost);

        return Map(shipment);
    }

    public ShipmentResponse GetById(Guid id) => Map(Require(id));

    public ShipmentResponse GetByTrackingCode(string trackingCode)
    {
        var shipment = _shipments.GetByTrackingCode(trackingCode)
            ?? throw new NotFoundException($"No existe un envio con codigo de rastreo {trackingCode}.");
        return Map(shipment);
    }

    public IReadOnlyList<ShipmentResponse> GetAll()
        => _shipments.GetAll().Select(Map).ToList();

    public ShipmentResponse Assign(Guid id, AssignShipmentRequest request)
    {
        var shipment = Require(id);
        var target = _assignment.Resolve(shipment, request.DriverId);

        shipment.AssignTo(target.DriverId, target.VehicleId, request.ChangedBy, _clock.Now);
        _shipments.Update(shipment);

        _logger.LogInformation(
            "Envio {TrackingCode} asignado al conductor {DriverId} (vehiculo {VehicleId}).",
            shipment.TrackingCode, target.DriverId, target.VehicleId);

        return Map(shipment);
    }

    public ShipmentResponse MarkInTransit(Guid id, StatusChangeRequest request)
    {
        var shipment = Require(id);
        shipment.MarkInTransit(request.ChangedBy, _clock.Now);
        _shipments.Update(shipment);
        return Map(shipment);
    }

    public ShipmentResponse MarkDelivered(Guid id, StatusChangeRequest request)
    {
        var shipment = Require(id);
        shipment.MarkDelivered(request.ChangedBy, _clock.Now);
        _shipments.Update(shipment);
        _logger.LogInformation("Envio {TrackingCode} marcado como entregado.", shipment.TrackingCode);
        return Map(shipment);
    }

    public ShipmentResponse Cancel(Guid id, CancelShipmentRequest request)
    {
        var shipment = Require(id);
        shipment.Cancel(request.Reason, request.ChangedBy, _clock.Now);
        _shipments.Update(shipment);
        _logger.LogInformation(
            "Envio {TrackingCode} cancelado. Motivo: {Reason}.", shipment.TrackingCode, request.Reason);
        return Map(shipment);
    }

    public IReadOnlyList<ShipmentResponse> GetDelayed(DateTime from, DateTime to)
    {
        var now = _clock.Now;
        return _shipments.GetAll()
            .Where(s => s.CreatedAt.Date >= from.Date && s.CreatedAt.Date <= to.Date)
            .Where(s => _sla.IsDelayed(s, now))
            .Select(Map)
            .ToList();
    }

    public TariffQuoteResponse Quote(CreateShipmentRequest request)
    {
        var package = new PackageDetails(
            request.Package.WeightKg,
            request.Package.LengthCm,
            request.Package.WidthCm,
            request.Package.HeightCm,
            request.Package.Type);

        var breakdown = _tariff.Calculate(package, request.Service, request.Origin, request.Destination);
        return new TariffQuoteResponse(
            breakdown.BaseRate,
            breakdown.WeightSurcharge,
            breakdown.DistanceSurcharge,
            breakdown.TypeSurcharge,
            breakdown.Total);
    }

    private Shipment Require(Guid id) =>
        _shipments.GetById(id) ?? throw new NotFoundException($"No existe un envio con id {id}.");

    private ShipmentResponse Map(Shipment shipment) =>
        ShipmentMapper.ToResponse(shipment, _sla, _clock.Now);

    private string GenerateUniqueTrackingCode()
    {
        for (var attempt = 0; attempt < MaxTrackingCodeAttempts; attempt++)
        {
            var candidate = _trackingCodes.Generate();
            if (!_shipments.TrackingCodeExists(candidate))
                return candidate;
        }

        // Extremadamente improbable con 8 digitos; se trata como conflicto para no fallar en silencio.
        throw new ConflictException("No fue posible generar un codigo de rastreo unico. Intente nuevamente.");
    }
}
