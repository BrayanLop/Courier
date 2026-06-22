namespace CourierMax.Domain.Entities;

/// <summary>
/// Tarifa por distancia entre un par de ciudades. La ruta es bidireccional:
/// Bogota-Medellin y Medellin-Bogota comparten distancia y recargo.
/// </summary>
public sealed class RouteRate
{
    public string CityA { get; }
    public string CityB { get; }
    public int DistanceKm { get; }
    public decimal DistanceSurcharge { get; }

    public RouteRate(string cityA, string cityB, int distanceKm, decimal distanceSurcharge)
    {
        CityA = cityA;
        CityB = cityB;
        DistanceKm = distanceKm;
        DistanceSurcharge = distanceSurcharge;
    }

    /// <summary>Indica si esta tarifa aplica al par de ciudades dado, sin importar el orden.</summary>
    public bool Matches(string origin, string destination)
    {
        return (string.Equals(CityA, origin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(CityB, destination, StringComparison.OrdinalIgnoreCase))
            || (string.Equals(CityA, destination, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(CityB, origin, StringComparison.OrdinalIgnoreCase));
    }
}
