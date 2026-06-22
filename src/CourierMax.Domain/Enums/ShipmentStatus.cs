namespace CourierMax.Domain.Enums;

/// <summary>
/// Estados por los que puede transitar un envio.
/// El flujo valido es CREADO -> ASIGNADO -> EN_TRANSITO -> ENTREGADO,
/// con CANCELADO accesible desde cualquier estado salvo ENTREGADO.
/// </summary>
public enum ShipmentStatus
{
    Creado = 0,
    Asignado = 1,
    EnTransito = 2,
    Entregado = 3,
    Cancelado = 4
}
