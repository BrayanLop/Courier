using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;

namespace CourierMax.Infrastructure.Reference;

public sealed class CityCatalog : ICityCatalog
{
    private readonly HashSet<string> _cities =
        new(ReferenceData.Cities, StringComparer.OrdinalIgnoreCase);

    public bool IsValid(string city)
        => !string.IsNullOrWhiteSpace(city) && _cities.Contains(city.Trim());

    public IReadOnlyCollection<string> All() => ReferenceData.Cities.ToList();
}

public sealed class RouteCatalog : IRouteCatalog
{
    private readonly IReadOnlyList<RouteRate> _routes = ReferenceData.Routes;

    public RouteRate? GetRate(string origin, string destination)
        => _routes.FirstOrDefault(r => r.Matches(origin, destination));
}
