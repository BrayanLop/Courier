using CourierMax.Domain.Enums;
using CourierMax.Domain.ValueObjects;

namespace CourierMax.Application.Services;

/// <summary>Desglose de la tarifa, util para trazabilidad y para validar el calculo.</summary>
public sealed record TariffBreakdown(
    decimal BaseRate,
    decimal WeightSurcharge,
    decimal DistanceSurcharge,
    decimal TypeSurcharge,
    decimal Total);

public interface ITariffCalculator
{
    TariffBreakdown Calculate(PackageDetails package, ServiceType service, string origin, string destination);
}
