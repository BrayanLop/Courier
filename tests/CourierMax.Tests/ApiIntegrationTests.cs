using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourierMax.Tests;

/// <summary>
/// Pruebas de integracion sobre el host real (pipeline HTTP completo: binding,
/// validacion y middleware de errores). Cubren que las entradas malformadas se
/// traduzcan a 400 y no a 500, y un smoke test del flujo de creacion (RF-01).
/// </summary>
public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static object ValidShipment(object? overridePackage = null, string service = "Express") => new
    {
        sender = new { name = "Ana", phone = "3001112233", address = "Calle 1" },
        recipient = new { name = "Luis", phone = "3104445566", address = "Carrera 2" },
        package = overridePackage ?? new { weightKg = 5, lengthCm = 30, widthCm = 30, heightCm = 30, type = "Paquete" },
        service,
        origin = "Bogota",
        destination = "Cali",
        createdBy = "ana"
    };

    [Fact]
    public async Task Create_with_invalid_package_type_returns_400_not_500()
    {
        var client = _factory.CreateClient();

        // "INVALIDO" no es un valor del enum: el binding falla antes de FluentValidation.
        var payload = ValidShipment(overridePackage: new
        {
            weightKg = 5,
            lengthCm = 30,
            widthCm = 30,
            heightCm = 30,
            type = "INVALIDO"
        });

        var response = await client.PostAsJsonAsync("/api/shipments", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_invalid_service_returns_400_not_500()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/shipments", ValidShipment(service: "SuperRapido"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_invalid_phone_returns_400()
    {
        var client = _factory.CreateClient();

        var payload = new
        {
            sender = new { name = "Ana", phone = "123", address = "Calle 1" },
            recipient = new { name = "Luis", phone = "3104445566", address = "Carrera 2" },
            package = new { weightKg = 5, lengthCm = 30, widthCm = 30, heightCm = 30, type = "Paquete" },
            service = "Express",
            origin = "Bogota",
            destination = "Cali"
        };

        var response = await client.PostAsJsonAsync("/api/shipments", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_valid_shipment_returns_201_with_tracking_code()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/shipments", ValidShipment());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreatedShipment>();
        body.Should().NotBeNull();
        body!.TrackingCode.Should().MatchRegex(@"^CM-\d{8}$");
        body.Status.Should().Be("Creado");
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreatedShipment(string TrackingCode, string Status);
}
