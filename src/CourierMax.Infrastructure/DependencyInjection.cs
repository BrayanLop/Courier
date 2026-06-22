using CourierMax.Application.Abstractions;
using CourierMax.Infrastructure.Persistence;
using CourierMax.Infrastructure.Reference;
using CourierMax.Infrastructure.Time;
using CourierMax.Infrastructure.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace CourierMax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Almacenes en memoria: singleton para que los datos persistan durante
        // toda la vida de la aplicacion.
        services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
        services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddSingleton<IDriverRepository, InMemoryDriverRepository>();

        // Catalogos de referencia.
        services.AddSingleton<ICityCatalog, CityCatalog>();
        services.AddSingleton<IRouteCatalog, RouteCatalog>();

        // Servicios tecnicos.
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITrackingCodeGenerator, TrackingCodeGenerator>();

        return services;
    }
}
