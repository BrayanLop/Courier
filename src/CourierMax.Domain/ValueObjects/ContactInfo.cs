namespace CourierMax.Domain.ValueObjects;

/// <summary>
/// Datos de un remitente o destinatario. Se modela como value object porque
/// no tiene identidad propia: dos contactos con los mismos datos son equivalentes.
/// </summary>
public sealed record ContactInfo
{
    public string Name { get; }
    public string Phone { get; }
    public string Address { get; }

    public ContactInfo(string name, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del contacto es obligatorio.", nameof(name));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("La direccion del contacto es obligatoria.", nameof(address));

        Name = name.Trim();
        Phone = phone?.Trim() ?? string.Empty;
        Address = address.Trim();
    }
}
