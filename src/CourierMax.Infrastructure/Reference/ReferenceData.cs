using CourierMax.Domain.Entities;

namespace CourierMax.Infrastructure.Reference;

/// <summary>
/// Datos de referencia del enunciado: ciudades, tarifas de distancia, vehiculos
/// y conductores. Se centralizan aqui para tener una unica fuente de verdad que
/// alimenta tanto los catalogos como el seed de los repositorios.
/// </summary>
public static class ReferenceData
{
    public static readonly IReadOnlyList<string> Cities = new[]
    {
        "Bogota", "Medellin", "Cali", "Barranquilla"
    };

    public static IReadOnlyList<RouteRate> Routes { get; } = new[]
    {
        new RouteRate("Bogota", "Medellin", 480, 12_000m),
        new RouteRate("Bogota", "Cali", 360, 9_000m),
        new RouteRate("Bogota", "Barranquilla", 950, 20_000m),
        new RouteRate("Medellin", "Cali", 310, 8_000m),
        new RouteRate("Medellin", "Barranquilla", 650, 15_000m),
        new RouteRate("Cali", "Barranquilla", 900, 18_000m)
    };

    public static IReadOnlyList<Vehicle> Vehicles { get; } = new[]
    {
        new Vehicle(1, "ABC123", 500m, 10m),
        new Vehicle(2, "DEF456", 300m, 6m),
        new Vehicle(3, "GHI789", 800m, 15m)
    };

    public static IReadOnlyList<Driver> Drivers { get; } = new[]
    {
        new Driver(1, "Juan Perez", 1),
        new Driver(2, "Maria Lopez", 2),
        new Driver(3, "Carlos Ruiz", 3)
    };
}
