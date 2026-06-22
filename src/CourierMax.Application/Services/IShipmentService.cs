using CourierMax.Application.DTOs;

namespace CourierMax.Application.Services;

public interface IShipmentService
{
    ShipmentResponse Create(CreateShipmentRequest request);
    ShipmentResponse GetById(Guid id);
    ShipmentResponse GetByTrackingCode(string trackingCode);
    IReadOnlyList<ShipmentResponse> GetAll();

    ShipmentResponse Assign(Guid id, AssignShipmentRequest request);
    ShipmentResponse MarkInTransit(Guid id, StatusChangeRequest request);
    ShipmentResponse MarkDelivered(Guid id, StatusChangeRequest request);
    ShipmentResponse Cancel(Guid id, CancelShipmentRequest request);

    /// <summary>Envios atrasados cuya fecha de creacion cae en el rango indicado (RF-05).</summary>
    IReadOnlyList<ShipmentResponse> GetDelayed(DateTime from, DateTime to);

    /// <summary>Cotiza la tarifa de un envio sin registrarlo (RF-04).</summary>
    TariffQuoteResponse Quote(CreateShipmentRequest request);
}
