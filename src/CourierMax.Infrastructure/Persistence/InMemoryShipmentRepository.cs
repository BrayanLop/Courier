using System.Collections.Concurrent;
using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;

namespace CourierMax.Infrastructure.Persistence;

/// <summary>
/// Almacen de envios en memoria, seguro para acceso concurrente. Como los envios
/// se guardan por referencia, Update no necesita reemplazar el objeto; se mantiene
/// por claridad semantica y para que el cambio a una persistencia real sea directo.
/// </summary>
public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly ConcurrentDictionary<Guid, Shipment> _store = new();

    public void Add(Shipment shipment) => _store[shipment.Id] = shipment;

    public void Update(Shipment shipment) => _store[shipment.Id] = shipment;

    public Shipment? GetById(Guid id) => _store.TryGetValue(id, out var shipment) ? shipment : null;

    public Shipment? GetByTrackingCode(string trackingCode)
        => _store.Values.FirstOrDefault(s =>
            string.Equals(s.TrackingCode, trackingCode, StringComparison.OrdinalIgnoreCase));

    public bool TrackingCodeExists(string trackingCode)
        => _store.Values.Any(s =>
            string.Equals(s.TrackingCode, trackingCode, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<Shipment> GetAll() => _store.Values.ToList();
}
