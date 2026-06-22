using CourierMax.Application.DTOs;
using CourierMax.Domain.Enums;
using CourierMax.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class MetricsServiceTests
{
    [Fact]
    public void Metrics_ForDriverWithoutShipments_AreZero()
    {
        var harness = new TestHarness();

        var metrics = harness.Metrics.GetForDriver(3);

        metrics.TotalAssigned.Should().Be(0);
        metrics.Delivered.Should().Be(0);
        metrics.OnTimePercentage.Should().Be(0);
        metrics.TotalWeightKg.Should().Be(0m);
    }

    [Fact]
    public void Metrics_ReflectDeliveredAndCancelledShipments()
    {
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));

        // Envio 1: entregado dentro del SLA por el conductor 1.
        var delivered = harness.ShipmentService.Create(
            TestHarness.ValidRequest(service: ServiceType.Express, weightKg: 50m));
        harness.ShipmentService.Assign(delivered.Id, new AssignShipmentRequest(1, "tester"));
        harness.Clock.Now = new DateTime(2026, 6, 23);
        harness.ShipmentService.MarkInTransit(delivered.Id, new StatusChangeRequest("tester"));
        harness.ShipmentService.MarkDelivered(delivered.Id, new StatusChangeRequest("tester"));

        // Envio 2: cancelado por el mismo conductor.
        harness.Clock.Now = new DateTime(2026, 6, 22);
        var cancelled = harness.ShipmentService.Create(
            TestHarness.ValidRequest(service: ServiceType.Estandar, weightKg: 10m));
        harness.ShipmentService.Assign(cancelled.Id, new AssignShipmentRequest(1, "tester"));
        harness.ShipmentService.Cancel(cancelled.Id, new CancelShipmentRequest("Cliente desistio", "tester"));

        var metrics = harness.Metrics.GetForDriver(1);

        metrics.TotalAssigned.Should().Be(2);
        metrics.Delivered.Should().Be(1);
        metrics.Cancelled.Should().Be(1);
        metrics.OnTimePercentage.Should().Be(100d);
        metrics.AverageDeliveryDays.Should().Be(1d);
        // Solo el envio entregado cuenta como peso transportado.
        metrics.TotalWeightKg.Should().Be(50m);
    }
}
