using CourierMax.Application.DTOs;

namespace CourierMax.Application.Services;

public interface IMetricsService
{
    DriverMetricsResponse GetForDriver(int driverId);
    IReadOnlyList<DriverMetricsResponse> GetAll();
}
