using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;
using CourierMax.Infrastructure.Reference;

namespace CourierMax.Infrastructure.Persistence;

/// <summary>
/// Vehiculos de la flota. Es un catalogo fijo (datos de referencia), por lo que
/// se expone como solo lectura sobre el seed.
/// </summary>
public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly IReadOnlyList<Vehicle> _vehicles = ReferenceData.Vehicles;

    public Vehicle? GetById(int id) => _vehicles.FirstOrDefault(v => v.Id == id);

    public IReadOnlyCollection<Vehicle> GetAll() => _vehicles.ToList();
}

public sealed class InMemoryDriverRepository : IDriverRepository
{
    private readonly IReadOnlyList<Driver> _drivers = ReferenceData.Drivers;

    public Driver? GetById(int id) => _drivers.FirstOrDefault(d => d.Id == id);

    public Driver? GetByVehicleId(int vehicleId) => _drivers.FirstOrDefault(d => d.VehicleId == vehicleId);

    public IReadOnlyCollection<Driver> GetAll() => _drivers.ToList();
}
