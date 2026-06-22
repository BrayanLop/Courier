using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using CourierMax.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CourierMax.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Servicios de dominio sin estado: seguros como singleton.
        services.AddSingleton<IBusinessDayCalculator, BusinessDayCalculator>();
        services.AddSingleton<ISlaService, SlaService>();
        services.AddSingleton<ITariffCalculator, TariffCalculator>();

        // Casos de uso.
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IMetricsService, MetricsService>();

        // Validadores de entrada (RN-04).
        services.AddScoped<IValidator<CreateShipmentRequest>, CreateShipmentRequestValidator>();
        services.AddScoped<IValidator<AssignShipmentRequest>, AssignShipmentRequestValidator>();
        services.AddScoped<IValidator<StatusChangeRequest>, StatusChangeRequestValidator>();
        services.AddScoped<IValidator<CancelShipmentRequest>, CancelShipmentRequestValidator>();

        return services;
    }
}
