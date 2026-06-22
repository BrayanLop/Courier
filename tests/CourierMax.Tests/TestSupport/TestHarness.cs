using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using CourierMax.Domain.Enums;
using CourierMax.Infrastructure.Persistence;
using CourierMax.Infrastructure.Reference;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourierMax.Tests.TestSupport;

/// <summary>
/// Arma el grafo de objetos real (servicios de dominio + repositorios en memoria)
/// para probar los flujos de negocio de extremo a extremo dentro de la capa de
/// aplicacion, sin necesidad de un contenedor de DI.
/// </summary>
public sealed class TestHarness
{
    public IShipmentRepository Shipments { get; }
    public IVehicleRepository Vehicles { get; }
    public IDriverRepository Drivers { get; }
    public TestClock Clock { get; }
    public StubTrackingCodeGenerator TrackingCodes { get; }
    public ISlaService Sla { get; }
    public IAssignmentService Assignment { get; }
    public IShipmentService ShipmentService { get; }
    public IMetricsService Metrics { get; }

    public TestHarness(DateTime? now = null, StubTrackingCodeGenerator? trackingCodes = null)
    {
        Shipments = new InMemoryShipmentRepository();
        Vehicles = new InMemoryVehicleRepository();
        Drivers = new InMemoryDriverRepository();
        Clock = new TestClock(now ?? new DateTime(2026, 6, 22)); // lunes habil por defecto
        TrackingCodes = trackingCodes ?? new StubTrackingCodeGenerator();

        var businessDays = new BusinessDayCalculator();
        Sla = new SlaService(businessDays);
        var tariff = new TariffCalculator(new RouteCatalog());
        Assignment = new AssignmentService(Shipments, Vehicles, Drivers);

        ShipmentService = new ShipmentService(
            Shipments, tariff, TrackingCodes, Assignment, Sla, Clock,
            NullLogger<ShipmentService>.Instance);

        Metrics = new MetricsService(Shipments, Drivers, Sla);
    }

    public static CreateShipmentRequest ValidRequest(
        ServiceType service = ServiceType.Estandar,
        PackageType packageType = PackageType.Paquete,
        decimal weightKg = 1m,
        string origin = "Bogota",
        string destination = "Medellin")
    {
        return new CreateShipmentRequest(
            new ContactDto("Remitente Prueba", "3001112233", "Calle 1 # 2-3"),
            new ContactDto("Destinatario Prueba", "3104445566", "Carrera 4 # 5-6"),
            new PackageDto(weightKg, 20m, 20m, 20m, packageType),
            service,
            origin,
            destination,
            "tester");
    }
}
