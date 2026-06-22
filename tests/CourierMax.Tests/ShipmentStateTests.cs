using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class ShipmentStateTests
{
    private static Shipment NewShipment()
    {
        var contact = new ContactInfo("Nombre", "3001112233", "Direccion 1");
        var package = new PackageDetails(2m, 20m, 20m, 20m, PackageType.Paquete);
        return new Shipment(
            Guid.NewGuid(), "CM-00000001", contact, contact, package,
            ServiceType.Estandar, "Bogota", "Medellin", 10_000m,
            new DateTime(2026, 6, 22), "tester");
    }

    private static readonly DateTime When = new(2026, 6, 22, 10, 0, 0);

    [Fact]
    public void NewShipment_StartsInCreatedWithInitialHistory()
    {
        var shipment = NewShipment();

        shipment.Status.Should().Be(ShipmentStatus.Creado);
        shipment.History.Should().ContainSingle();
        shipment.History[0].To.Should().Be(ShipmentStatus.Creado);
        shipment.History[0].From.Should().BeNull();
    }

    [Fact]
    public void HappyPath_TraversesAllForwardStates()
    {
        var shipment = NewShipment();

        shipment.AssignTo(1, 1, "tester", When);
        shipment.Status.Should().Be(ShipmentStatus.Asignado);
        shipment.AssignedDriverId.Should().Be(1);
        shipment.OccupiesVehicleCapacity.Should().BeTrue();

        shipment.MarkInTransit("tester", When);
        shipment.Status.Should().Be(ShipmentStatus.EnTransito);

        shipment.MarkDelivered("tester", When);
        shipment.Status.Should().Be(ShipmentStatus.Entregado);
        shipment.OccupiesVehicleCapacity.Should().BeFalse();
        shipment.History.Should().HaveCount(4);
    }

    [Fact]
    public void MarkInTransit_FromCreated_IsRejected()
    {
        var shipment = NewShipment();

        var act = () => shipment.MarkInTransit("tester", When);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Deliver_FromAssigned_IsRejected()
    {
        var shipment = NewShipment();
        shipment.AssignTo(1, 1, "tester", When);

        var act = () => shipment.MarkDelivered("tester", When);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Cancel_AfterDelivered_IsRejected()
    {
        var shipment = NewShipment();
        shipment.AssignTo(1, 1, "tester", When);
        shipment.MarkInTransit("tester", When);
        shipment.MarkDelivered("tester", When);

        var act = () => shipment.Cancel("Cliente ya no requiere el envio", "tester", When);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Cancel_WithShortReason_IsRejected()
    {
        var shipment = NewShipment();

        var act = () => shipment.Cancel("no", "tester", When);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_FromAssigned_ReleasesVehicleCapacity()
    {
        var shipment = NewShipment();
        shipment.AssignTo(1, 1, "tester", When);

        shipment.Cancel("Direccion incorrecta del destinatario", "tester", When);

        shipment.Status.Should().Be(ShipmentStatus.Cancelado);
        shipment.OccupiesVehicleCapacity.Should().BeFalse();
        shipment.History.Last().Reason.Should().Contain("Direccion incorrecta");
    }
}
