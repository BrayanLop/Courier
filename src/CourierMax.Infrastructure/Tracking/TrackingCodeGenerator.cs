using CourierMax.Application.Abstractions;

namespace CourierMax.Infrastructure.Tracking;

/// <summary>
/// Genera codigos candidatos con formato CM-XXXXXXXX (8 digitos). La unicidad
/// final se garantiza en el servicio, que reintenta contra el repositorio (RN-05).
/// </summary>
public sealed class TrackingCodeGenerator : ITrackingCodeGenerator
{
    public string Generate()
    {
        var number = Random.Shared.Next(0, 100_000_000); // 0 .. 99.999.999
        return $"CM-{number:D8}";
    }
}
