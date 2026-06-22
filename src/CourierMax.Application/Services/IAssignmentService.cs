using CourierMax.Domain.Entities;

namespace CourierMax.Application.Services;

/// <summary>Conductor y vehiculo resueltos para una asignacion.</summary>
public sealed record AssignmentTarget(int DriverId, int VehicleId);

public interface IAssignmentService
{
    /// <summary>
    /// Resuelve a que conductor/vehiculo se asigna el envio respetando la
    /// capacidad disponible. Si driverId es nulo aplica balanceo de carga.
    /// Lanza una excepcion de dominio si no hay capacidad o el conductor no es valido.
    /// </summary>
    AssignmentTarget Resolve(Shipment shipment, int? driverId);
}
