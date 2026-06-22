using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;

namespace CourierMax.Application.Services;

public interface ISlaService
{
    int GetSlaBusinessDays(ServiceType service);

    /// <summary>Fecha limite de entrega segun el SLA del servicio, contada en dias habiles.</summary>
    DateTime GetDeadline(DateTime createdAt, ServiceType service);

    /// <summary>Indica si el envio esta atrasado a la fecha indicada (no entregado pasado su SLA).</summary>
    bool IsDelayed(Shipment shipment, DateTime asOf);
}
