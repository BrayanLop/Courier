using CourierMax.Application.Abstractions;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;

namespace CourierMax.Application.Services;

/// <summary>
/// Calcula el costo de un envio (RF-04): tarifa base por servicio, recargo por
/// peso adicional a los primeros 2 kg, recargo por distancia entre ciudades y
/// recargo porcentual por tipo de paquete aplicado sobre el subtotal.
/// </summary>
public sealed class TariffCalculator : ITariffCalculator
{
    private const decimal FreeWeightKg = 2m;
    private const decimal SurchargePerExtraKg = 1_500m;

    private readonly IRouteCatalog _routes;

    public TariffCalculator(IRouteCatalog routes)
    {
        _routes = routes;
    }

    public TariffBreakdown Calculate(PackageDetails package, ServiceType service, string origin, string destination)
    {
        var baseRate = GetBaseRate(service);

        var extraKg = Math.Max(0m, package.WeightKg - FreeWeightKg);
        var weightSurcharge = extraKg * SurchargePerExtraKg;

        var distanceSurcharge = GetDistanceSurcharge(origin, destination);

        var subtotal = baseRate + weightSurcharge + distanceSurcharge;
        var typeSurcharge = subtotal * GetTypeSurchargeRate(package.Type);

        var total = Round(subtotal + typeSurcharge);

        return new TariffBreakdown(
            baseRate,
            Round(weightSurcharge),
            distanceSurcharge,
            Round(typeSurcharge),
            total);
    }

    private static decimal GetBaseRate(ServiceType service) => service switch
    {
        ServiceType.Estandar => 8_000m,
        ServiceType.Express => 15_000m,
        ServiceType.MismoDia => 25_000m,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Tipo de servicio no reconocido.")
    };

    private static decimal GetTypeSurchargeRate(PackageType type) => type switch
    {
        PackageType.Fragil => 0.30m,
        PackageType.Perecedero => 0.25m,
        PackageType.Documento => 0m,
        PackageType.Paquete => 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paquete no reconocido.")
    };

    private decimal GetDistanceSurcharge(string origin, string destination)
    {
        // Un envio dentro de la misma ciudad no tiene recargo por distancia.
        if (string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase))
            return 0m;

        var route = _routes.GetRate(origin, destination)
            ?? throw new BusinessRuleException(
                $"No existe una tarifa de distancia definida entre {origin} y {destination}.");

        return route.DistanceSurcharge;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
