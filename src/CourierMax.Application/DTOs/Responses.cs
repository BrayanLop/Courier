using CourierMax.Domain.Enums;

namespace CourierMax.Application.DTOs;

public sealed record StatusChangeResponse(
    ShipmentStatus? From,
    ShipmentStatus To,
    DateTime ChangedAt,
    string ChangedBy,
    string? Reason);

public sealed record ShipmentResponse(
    Guid Id,
    string TrackingCode,
    ShipmentStatus Status,
    ServiceType Service,
    string Origin,
    string Destination,
    decimal Cost,
    DateTime CreatedAt,
    DateTime Deadline,
    bool IsDelayed,
    ContactDto Sender,
    ContactDto Recipient,
    PackageDto Package,
    int? AssignedDriverId,
    int? AssignedVehicleId,
    IReadOnlyList<StatusChangeResponse> History);

/// <summary>Cotizacion de tarifa con su desglose (RF-04).</summary>
public sealed record TariffQuoteResponse(
    decimal BaseRate,
    decimal WeightSurcharge,
    decimal DistanceSurcharge,
    decimal TypeSurcharge,
    decimal Total);

/// <summary>Reporte de eficiencia por conductor (RF-06).</summary>
public sealed record DriverMetricsResponse(
    int DriverId,
    string DriverName,
    int TotalAssigned,
    int Delivered,
    int Cancelled,
    int InTransit,
    double AverageDeliveryDays,
    double OnTimePercentage,
    decimal TotalWeightKg);
