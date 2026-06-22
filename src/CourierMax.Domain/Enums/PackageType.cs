namespace CourierMax.Domain.Enums;

/// <summary>
/// Tipo de paquete. Influye en el recargo aplicado sobre el subtotal de la tarifa.
/// </summary>
public enum PackageType
{
    Documento = 0,
    Paquete = 1,
    Fragil = 2,
    Perecedero = 3
}
