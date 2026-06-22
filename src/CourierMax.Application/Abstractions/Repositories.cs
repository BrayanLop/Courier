using CourierMax.Domain.Entities;

namespace CourierMax.Application.Abstractions;

/// <summary>
/// Persistencia de envios. La implementacion actual es en memoria, pero la
/// interfaz permite sustituirla por EF Core/Dapper sin tocar los servicios.
/// Como los envios son objetos mutables, las modificaciones quedan reflejadas
/// sin un metodo Update explicito; este se incluye para mantener la semantica
/// clara de cara a una persistencia real.
/// </summary>
public interface IShipmentRepository
{
    void Add(Shipment shipment);
    void Update(Shipment shipment);
    Shipment? GetById(Guid id);
    Shipment? GetByTrackingCode(string trackingCode);
    bool TrackingCodeExists(string trackingCode);
    IReadOnlyCollection<Shipment> GetAll();
}

public interface IVehicleRepository
{
    Vehicle? GetById(int id);
    IReadOnlyCollection<Vehicle> GetAll();
}

public interface IDriverRepository
{
    Driver? GetById(int id);
    Driver? GetByVehicleId(int vehicleId);
    IReadOnlyCollection<Driver> GetAll();
}

/// <summary>Catalogo de ciudades validas del sistema (RN-04).</summary>
public interface ICityCatalog
{
    bool IsValid(string city);
    IReadOnlyCollection<string> All();
}

/// <summary>Catalogo de tarifas por distancia entre ciudades (RF-04).</summary>
public interface IRouteCatalog
{
    RouteRate? GetRate(string origin, string destination);
}
