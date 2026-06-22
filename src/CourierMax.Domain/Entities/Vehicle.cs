namespace CourierMax.Domain.Entities;

/// <summary>
/// Vehiculo de la flota. Define la capacidad maxima en peso y volumen que el
/// motor de asignacion no puede sobrepasar (RN-01).
/// </summary>
public sealed class Vehicle
{
    public int Id { get; }
    public string Plate { get; }
    public decimal MaxWeightKg { get; }
    public decimal MaxVolumeM3 { get; }

    public Vehicle(int id, string plate, decimal maxWeightKg, decimal maxVolumeM3)
    {
        Id = id;
        Plate = plate;
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
    }
}
