using CourierMax.Application.Services;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using CourierMax.Infrastructure.Persistence;
using CourierMax.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class AssignmentServiceTests
{
    private static int _sequence;

    private static Shipment ShipmentWeighing(decimal weightKg, decimal sideCm = 20m)
    {
        var contact = new ContactInfo("Nombre", "3001112233", "Direccion 1");
        var package = new PackageDetails(weightKg, sideCm, sideCm, sideCm, PackageType.Paquete);
        var code = $"CM-{Interlocked.Increment(ref _sequence):D8}";
        return new Shipment(
            Guid.NewGuid(), code,
            contact, contact, package, ServiceType.Estandar, "Bogota", "Medellin",
            10_000m, new DateTime(2026, 6, 22), "tester");
    }

    private static Shipment AssignedTo(decimal weightKg, int driverId, int vehicleId)
    {
        var shipment = ShipmentWeighing(weightKg);
        shipment.AssignTo(driverId, vehicleId, "tester", new DateTime(2026, 6, 22));
        return shipment;
    }

    [Fact]
    public void Resolve_WhenWeightExceedsCapacity_Throws()
    {
        var shipments = new InMemoryShipmentRepository();
        // Vehiculo de 300 kg ya cargado con 250 kg.
        shipments.Add(AssignedTo(250m, driverId: 1, vehicleId: 1));

        var service = new AssignmentService(
            shipments,
            new ListVehicleRepository(new Vehicle(1, "AAA111", 300m, 10m)),
            new ListDriverRepository(new Driver(1, "Conductor", 1)));

        var act = () => service.Resolve(ShipmentWeighing(100m), driverId: 1);

        act.Should().Throw<ConflictException>()
            .WithMessage("*peso*");
    }

    [Fact]
    public void Resolve_WhenVolumeExceedsCapacity_Throws()
    {
        var shipments = new InMemoryShipmentRepository();
        var service = new AssignmentService(
            shipments,
            // Capacidad de peso amplia pero de volumen minima.
            new ListVehicleRepository(new Vehicle(1, "AAA111", 1000m, 0.001m)),
            new ListDriverRepository(new Driver(1, "Conductor", 1)));

        // 50x50x50 cm = 0.125 m3, supera el limite de 0.001 m3.
        var bulky = ShipmentWeighing(1m, sideCm: 50m);

        var act = () => service.Resolve(bulky, driverId: 1);

        act.Should().Throw<ConflictException>()
            .WithMessage("*volumen*");
    }

    [Fact]
    public void Resolve_InactiveDriver_Throws()
    {
        var service = new AssignmentService(
            new InMemoryShipmentRepository(),
            new ListVehicleRepository(new Vehicle(1, "AAA111", 500m, 10m)),
            new ListDriverRepository(new Driver(1, "Inactivo", 1, isActive: false)));

        var act = () => service.Resolve(ShipmentWeighing(10m), driverId: 1);

        act.Should().Throw<ConflictException>()
            .WithMessage("*no esta activo*");
    }

    [Fact]
    public void Resolve_UnknownDriver_ThrowsNotFound()
    {
        var service = new AssignmentService(
            new InMemoryShipmentRepository(),
            new ListVehicleRepository(new Vehicle(1, "AAA111", 500m, 10m)),
            new ListDriverRepository(new Driver(1, "Conductor", 1)));

        var act = () => service.Resolve(ShipmentWeighing(10m), driverId: 99);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void Resolve_WithLoadBalancing_PicksLeastLoadedVehicle()
    {
        var shipments = new InMemoryShipmentRepository();
        // Vehiculo 1 ya tiene 200 kg; vehiculo 2 esta vacio.
        shipments.Add(AssignedTo(200m, driverId: 1, vehicleId: 1));

        var service = new AssignmentService(
            shipments,
            new ListVehicleRepository(
                new Vehicle(1, "AAA111", 500m, 10m),
                new Vehicle(2, "BBB222", 500m, 10m)),
            new ListDriverRepository(
                new Driver(1, "Conductor 1", 1),
                new Driver(2, "Conductor 2", 2)));

        var target = service.Resolve(ShipmentWeighing(50m), driverId: null);

        // Debe balancear hacia el vehiculo con menor carga (el 2).
        target.VehicleId.Should().Be(2);
        target.DriverId.Should().Be(2);
    }

    [Fact]
    public void Resolve_WhenNoVehicleFits_Throws()
    {
        var shipments = new InMemoryShipmentRepository();
        var service = new AssignmentService(
            shipments,
            new ListVehicleRepository(new Vehicle(1, "AAA111", 30m, 10m)),
            new ListDriverRepository(new Driver(1, "Conductor", 1)));

        var act = () => service.Resolve(ShipmentWeighing(100m), driverId: null);

        act.Should().Throw<ConflictException>()
            .WithMessage("*capacidad suficiente*");
    }
}
