using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;

namespace CourierMax.Tests.TestSupport;

/// <summary>Repositorio de vehiculos respaldado por una lista, para escenarios controlados.</summary>
public sealed class ListVehicleRepository : IVehicleRepository
{
    private readonly List<Vehicle> _vehicles;
    public ListVehicleRepository(params Vehicle[] vehicles) => _vehicles = vehicles.ToList();

    public Vehicle? GetById(int id) => _vehicles.FirstOrDefault(v => v.Id == id);
    public IReadOnlyCollection<Vehicle> GetAll() => _vehicles;
}

/// <summary>Repositorio de conductores respaldado por una lista, para escenarios controlados.</summary>
public sealed class ListDriverRepository : IDriverRepository
{
    private readonly List<Driver> _drivers;
    public ListDriverRepository(params Driver[] drivers) => _drivers = drivers.ToList();

    public Driver? GetById(int id) => _drivers.FirstOrDefault(d => d.Id == id);
    public Driver? GetByVehicleId(int vehicleId) => _drivers.FirstOrDefault(d => d.VehicleId == vehicleId);
    public IReadOnlyCollection<Driver> GetAll() => _drivers;
}
