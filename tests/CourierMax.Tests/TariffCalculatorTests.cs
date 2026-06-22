using CourierMax.Application.Services;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using CourierMax.Infrastructure.Reference;
using FluentAssertions;
using Xunit;

namespace CourierMax.Tests;

public class TariffCalculatorTests
{
    private readonly TariffCalculator _calculator = new(new RouteCatalog());

    [Fact]
    public void Calculate_FragileExpress_MatchesWorkedExampleFromSpec()
    {
        // Ejemplo del enunciado: fragil 5 kg, express, Bogota -> Medellin = 40.950.
        var package = new PackageDetails(5m, 30m, 30m, 30m, PackageType.Fragil);

        var result = _calculator.Calculate(package, ServiceType.Express, "Bogota", "Medellin");

        result.BaseRate.Should().Be(15_000m);
        result.WeightSurcharge.Should().Be(4_500m);   // 3 kg extra x 1.500
        result.DistanceSurcharge.Should().Be(12_000m);
        result.TypeSurcharge.Should().Be(9_450m);      // (15.000 + 4.500 + 12.000) x 30%
        result.Total.Should().Be(40_950m);
    }

    [Fact]
    public void Calculate_LightStandardDocument_HasNoSurcharges()
    {
        var package = new PackageDetails(1.5m, 10m, 10m, 10m, PackageType.Documento);

        var result = _calculator.Calculate(package, ServiceType.Estandar, "Medellin", "Cali");

        result.BaseRate.Should().Be(8_000m);
        result.WeightSurcharge.Should().Be(0m);        // por debajo de 2 kg
        result.DistanceSurcharge.Should().Be(8_000m);
        result.TypeSurcharge.Should().Be(0m);
        result.Total.Should().Be(16_000m);
    }

    [Fact]
    public void Calculate_Perishable_AppliesTwentyFivePercentSurcharge()
    {
        var package = new PackageDetails(2m, 10m, 10m, 10m, PackageType.Perecedero);

        var result = _calculator.Calculate(package, ServiceType.MismoDia, "Bogota", "Cali");

        // Subtotal = 25.000 + 0 + 9.000 = 34.000 ; recargo 25% = 8.500 ; total = 42.500
        result.TypeSurcharge.Should().Be(8_500m);
        result.Total.Should().Be(42_500m);
    }

    [Fact]
    public void Calculate_RouteIsBidirectional()
    {
        var package = new PackageDetails(1m, 10m, 10m, 10m, PackageType.Paquete);

        var forward = _calculator.Calculate(package, ServiceType.Estandar, "Bogota", "Medellin");
        var backward = _calculator.Calculate(package, ServiceType.Estandar, "Medellin", "Bogota");

        backward.Total.Should().Be(forward.Total);
    }

    [Fact]
    public void Calculate_SameCity_HasNoDistanceSurcharge()
    {
        // Borde: un envio dentro de la misma ciudad no tiene recargo por distancia.
        var package = new PackageDetails(1m, 10m, 10m, 10m, PackageType.Paquete);

        var result = _calculator.Calculate(package, ServiceType.Estandar, "Bogota", "Bogota");

        result.DistanceSurcharge.Should().Be(0m);
        result.Total.Should().Be(8_000m);
    }

    [Fact]
    public void Calculate_HeavyPackage_ChargesPerExtraKilogram()
    {
        var package = new PackageDetails(10m, 10m, 10m, 10m, PackageType.Paquete);

        var result = _calculator.Calculate(package, ServiceType.Estandar, "Bogota", "Cali");

        // 8 kg extra x 1.500 = 12.000
        result.WeightSurcharge.Should().Be(12_000m);
    }
}
