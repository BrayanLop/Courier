using CourierMax.Application.Abstractions;

namespace CourierMax.Infrastructure.Time;

/// <summary>Reloj de produccion. La hora se maneja en horario local (Colombia).</summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
