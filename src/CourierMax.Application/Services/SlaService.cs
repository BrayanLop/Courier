using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;

namespace CourierMax.Application.Services;

/// <summary>
/// Resuelve el SLA de cada servicio en dias habiles y determina si un envio
/// esta atrasado (RF-05). Un envio entregado o cancelado nunca se considera atrasado.
/// </summary>
public sealed class SlaService : ISlaService
{
    private readonly IBusinessDayCalculator _businessDays;

    public SlaService(IBusinessDayCalculator businessDays)
    {
        _businessDays = businessDays;
    }

    public int GetSlaBusinessDays(ServiceType service) => service switch
    {
        ServiceType.Estandar => 5,
        ServiceType.Express => 2,
        ServiceType.MismoDia => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Tipo de servicio no reconocido.")
    };

    public DateTime GetDeadline(DateTime createdAt, ServiceType service)
        => _businessDays.AddBusinessDays(createdAt, GetSlaBusinessDays(service));

    public bool IsDelayed(Shipment shipment, DateTime asOf)
    {
        if (shipment.Status is ShipmentStatus.Entregado or ShipmentStatus.Cancelado)
            return false;

        var deadline = GetDeadline(shipment.CreatedAt, shipment.Service);
        return asOf.Date > deadline.Date;
    }
}
