namespace CourierMax.Domain.Entities;

/// <summary>
/// Conductor de la flota. Mantiene una relacion 1:1 con un vehiculo (RF-03).
/// Solo los conductores activos pueden recibir asignaciones.
/// </summary>
public sealed class Driver
{
    public int Id { get; }
    public string Name { get; }
    public int VehicleId { get; }
    public bool IsActive { get; private set; }

    public Driver(int id, string name, int vehicleId, bool isActive = true)
    {
        Id = id;
        Name = name;
        VehicleId = vehicleId;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
