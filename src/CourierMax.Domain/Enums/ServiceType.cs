namespace CourierMax.Domain.Enums;

/// <summary>
/// Tipo de servicio contratado. Determina la tarifa base y el SLA en dias habiles.
/// </summary>
public enum ServiceType
{
    Estandar = 0,
    Express = 1,
    MismoDia = 2
}
