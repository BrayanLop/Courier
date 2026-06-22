using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using CourierMax.Domain.Enums;
using CourierMax.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class SlaServiceTests
{
    [Theory]
    [InlineData(ServiceType.Estandar, 5)]
    [InlineData(ServiceType.Express, 2)]
    [InlineData(ServiceType.MismoDia, 0)]
    public void GetSlaBusinessDays_ReturnsConfiguredSla(ServiceType service, int expected)
    {
        var sla = new SlaService(new BusinessDayCalculator());

        sla.GetSlaBusinessDays(service).Should().Be(expected);
    }

    [Fact]
    public void IsDelayed_ExpressPastDeadline_ReturnsTrue()
    {
        // Creado el lunes 22; SLA express = 2 dias habiles -> limite miercoles 24.
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));
        var response = harness.ShipmentService.Create(
            TestHarness.ValidRequest(service: ServiceType.Express));
        var shipment = harness.Shipments.GetById(response.Id)!;

        var delayed = harness.Sla.IsDelayed(shipment, new DateTime(2026, 6, 25));

        delayed.Should().BeTrue();
    }

    [Fact]
    public void IsDelayed_WithinDeadline_ReturnsFalse()
    {
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));
        var response = harness.ShipmentService.Create(
            TestHarness.ValidRequest(service: ServiceType.Express));
        var shipment = harness.Shipments.GetById(response.Id)!;

        harness.Sla.IsDelayed(shipment, new DateTime(2026, 6, 23)).Should().BeFalse();
    }

    [Fact]
    public void IsDelayed_Delivered_IsNeverDelayed()
    {
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));
        var created = harness.ShipmentService.Create(
            TestHarness.ValidRequest(service: ServiceType.Express));

        // Recorrer el flujo completo hasta entregado.
        harness.ShipmentService.Assign(created.Id, new AssignShipmentRequest(null, "tester"));
        harness.ShipmentService.MarkInTransit(created.Id, new StatusChangeRequest("tester"));
        harness.ShipmentService.MarkDelivered(created.Id, new StatusChangeRequest("tester"));

        var shipment = harness.Shipments.GetById(created.Id)!;
        harness.Sla.IsDelayed(shipment, new DateTime(2026, 7, 30)).Should().BeFalse();
    }
}
