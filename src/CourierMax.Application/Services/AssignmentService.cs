using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Exceptions;

namespace CourierMax.Application.Services;

/// <summary>
/// Decide la asignacion de un envio a un conductor/vehiculo (RF-03) garantizando
/// que no se exceda la capacidad de peso ni de volumen (RN-01). Cuando no se
/// especifica conductor, selecciona entre los vehiculos con capacidad disponible
/// el que tenga menor carga actual (balanceo de carga).
/// </summary>
public sealed class AssignmentService : IAssignmentService
{
    private readonly IShipmentRepository _shipments;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;

    public AssignmentService(
        IShipmentRepository shipments,
        IVehicleRepository vehicles,
        IDriverRepository drivers)
    {
        _shipments = shipments;
        _vehicles = vehicles;
        _drivers = drivers;
    }

    public AssignmentTarget Resolve(Shipment shipment, int? driverId)
    {
        return driverId.HasValue
            ? ResolveForSpecificDriver(shipment, driverId.Value)
            : ResolveWithLoadBalancing(shipment);
    }

    private AssignmentTarget ResolveForSpecificDriver(Shipment shipment, int driverId)
    {
        var driver = _drivers.GetById(driverId)
            ?? throw new NotFoundException($"No existe un conductor con id {driverId}.");

        if (!driver.IsActive)
            throw new ConflictException($"El conductor {driver.Name} no esta activo y no puede recibir asignaciones.");

        var vehicle = _vehicles.GetById(driver.VehicleId)
            ?? throw new NotFoundException($"El conductor {driver.Name} no tiene un vehiculo asociado valido.");

        var (weightUsed, volumeUsed) = CurrentLoad(vehicle.Id);

        if (weightUsed + shipment.Package.WeightKg > vehicle.MaxWeightKg)
            throw new ConflictException(
                $"Capacidad de peso excedida para el vehiculo {vehicle.Plate}: " +
                $"carga actual {weightUsed} kg + {shipment.Package.WeightKg} kg supera el maximo de {vehicle.MaxWeightKg} kg.");

        if (volumeUsed + shipment.Package.VolumeM3 > vehicle.MaxVolumeM3)
            throw new ConflictException(
                $"Capacidad de volumen excedida para el vehiculo {vehicle.Plate}: " +
                $"carga actual {volumeUsed:0.###} m3 + {shipment.Package.VolumeM3:0.###} m3 supera el maximo de {vehicle.MaxVolumeM3} m3.");

        return new AssignmentTarget(driver.Id, vehicle.Id);
    }

    private AssignmentTarget ResolveWithLoadBalancing(Shipment shipment)
    {
        var candidates = _drivers.GetAll()
            .Where(d => d.IsActive)
            .Select(d => new { Driver = d, Vehicle = _vehicles.GetById(d.VehicleId) })
            .Where(x => x.Vehicle is not null)
            .Select(x => new
            {
                x.Driver,
                Vehicle = x.Vehicle!,
                Load = CurrentLoad(x.Vehicle!.Id)
            })
            .Where(x => Fits(x.Vehicle, x.Load, shipment))
            // Balanceo: menor carga de peso actual primero.
            .OrderBy(x => x.Load.Weight)
            .ThenBy(x => x.Vehicle.Id)
            .ToList();

        var selected = candidates.FirstOrDefault()
            ?? throw new ConflictException(
                "No hay ningun vehiculo activo con capacidad suficiente para este envio.");

        return new AssignmentTarget(selected.Driver.Id, selected.Vehicle.Id);
    }

    private static bool Fits(Vehicle vehicle, (decimal Weight, decimal Volume) load, Shipment shipment)
        => load.Weight + shipment.Package.WeightKg <= vehicle.MaxWeightKg
        && load.Volume + shipment.Package.VolumeM3 <= vehicle.MaxVolumeM3;

    private (decimal Weight, decimal Volume) CurrentLoad(int vehicleId)
    {
        var occupying = _shipments.GetAll()
            .Where(s => s.AssignedVehicleId == vehicleId && s.OccupiesVehicleCapacity)
            .ToList();

        var weight = occupying.Sum(s => s.Package.WeightKg);
        var volume = occupying.Sum(s => s.Package.VolumeM3);
        return (weight, volume);
    }
}
