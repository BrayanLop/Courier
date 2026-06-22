using CourierMax.Domain.Enums;

namespace CourierMax.Domain.ValueObjects;

/// <summary>
/// Caracteristicas fisicas del paquete. Expone el volumen en metros cubicos,
/// calculado a partir de las dimensiones en centimetros, que es la unidad que
/// usa la capacidad de los vehiculos.
/// </summary>
public sealed record PackageDetails
{
    public decimal WeightKg { get; }
    public decimal LengthCm { get; }
    public decimal WidthCm { get; }
    public decimal HeightCm { get; }
    public PackageType Type { get; }

    public PackageDetails(decimal weightKg, decimal lengthCm, decimal widthCm, decimal heightCm, PackageType type)
    {
        if (weightKg <= 0)
            throw new ArgumentException("El peso debe ser mayor a cero.", nameof(weightKg));
        if (lengthCm <= 0 || widthCm <= 0 || heightCm <= 0)
            throw new ArgumentException("Las dimensiones deben ser mayores a cero.");

        WeightKg = weightKg;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
        Type = type;
    }

    /// <summary>
    /// Volumen en m3. 1 m3 = 1.000.000 cm3.
    /// </summary>
    public decimal VolumeM3 => (LengthCm * WidthCm * HeightCm) / 1_000_000m;
}
