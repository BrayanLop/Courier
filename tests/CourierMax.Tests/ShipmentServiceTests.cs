using System.Text.RegularExpressions;
using CourierMax.Application.DTOs;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class ShipmentServiceTests
{
    [Fact]
    public void Create_AssignsTrackingCodeAndInitialState()
    {
        var harness = new TestHarness();

        var response = harness.ShipmentService.Create(TestHarness.ValidRequest());

        response.Status.Should().Be(ShipmentStatus.Creado);
        response.TrackingCode.Should().MatchRegex(@"^CM-\d{8}$");
        response.Cost.Should().BeGreaterThan(0m);
        response.History.Should().ContainSingle();
    }

    [Fact]
    public void Create_GeneratesUniqueTrackingCode_WhenFirstCandidateCollides()
    {
        // El generador propone el mismo codigo dos veces; el servicio debe reintentar (RN-05).
        var generator = new StubTrackingCodeGenerator("CM-00000001", "CM-00000001", "CM-00000002");
        var harness = new TestHarness(trackingCodes: generator);

        var first = harness.ShipmentService.Create(TestHarness.ValidRequest());
        var second = harness.ShipmentService.Create(TestHarness.ValidRequest());

        first.TrackingCode.Should().Be("CM-00000001");
        second.TrackingCode.Should().Be("CM-00000002");
    }

    [Fact]
    public void FullLifecycle_FromCreationToDelivery_Succeeds()
    {
        var harness = new TestHarness();
        var created = harness.ShipmentService.Create(TestHarness.ValidRequest(weightKg: 50m));

        var assigned = harness.ShipmentService.Assign(created.Id, new AssignShipmentRequest(null, "tester"));
        assigned.Status.Should().Be(ShipmentStatus.Asignado);
        assigned.AssignedDriverId.Should().NotBeNull();

        harness.ShipmentService.MarkInTransit(created.Id, new StatusChangeRequest("tester"));
        var delivered = harness.ShipmentService.MarkDelivered(created.Id, new StatusChangeRequest("tester"));

        delivered.Status.Should().Be(ShipmentStatus.Entregado);
        delivered.History.Should().HaveCount(4);
    }

    [Fact]
    public void Cancel_SetsStatusAndRecordsReason()
    {
        var harness = new TestHarness();
        var created = harness.ShipmentService.Create(TestHarness.ValidRequest());

        var cancelled = harness.ShipmentService.Cancel(
            created.Id, new CancelShipmentRequest("El cliente cancelo la compra", "tester"));

        cancelled.Status.Should().Be(ShipmentStatus.Cancelado);
        cancelled.History.Last().Reason.Should().Be("El cliente cancelo la compra");
    }

    [Fact]
    public void GetById_UnknownId_ThrowsNotFound()
    {
        var harness = new TestHarness();

        var act = () => harness.ShipmentService.GetById(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void GetDelayed_ReturnsExpressShipmentPastDeadline()
    {
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));
        var created = harness.ShipmentService.Create(TestHarness.ValidRequest(service: ServiceType.Express));

        // Avanza el reloj mas alla del limite (miercoles 24) sin entregar.
        harness.Clock.Now = new DateTime(2026, 6, 25);

        var delayed = harness.ShipmentService.GetDelayed(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        delayed.Should().ContainSingle(s => s.Id == created.Id);
        delayed.Single().IsDelayed.Should().BeTrue();
    }

    [Fact]
    public void GetDelayed_ExcludesDeliveredShipments()
    {
        var harness = new TestHarness(now: new DateTime(2026, 6, 22));
        var created = harness.ShipmentService.Create(TestHarness.ValidRequest(service: ServiceType.Express));
        harness.ShipmentService.Assign(created.Id, new AssignShipmentRequest(null, "tester"));
        harness.ShipmentService.MarkInTransit(created.Id, new StatusChangeRequest("tester"));
        harness.ShipmentService.MarkDelivered(created.Id, new StatusChangeRequest("tester"));

        harness.Clock.Now = new DateTime(2026, 6, 25);

        var delayed = harness.ShipmentService.GetDelayed(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        delayed.Should().BeEmpty();
    }
}
