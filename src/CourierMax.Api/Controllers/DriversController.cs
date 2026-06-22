using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

[ApiController]
[Route("api/drivers")]
[Produces("application/json")]
public sealed class DriversController : ControllerBase
{
    private readonly IMetricsService _metrics;
    private readonly IDriverRepository _drivers;

    public DriversController(IMetricsService metrics, IDriverRepository drivers)
    {
        _metrics = metrics;
        _drivers = drivers;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
        => Ok(_drivers.GetAll().Select(d => new { d.Id, d.Name, d.VehicleId, d.IsActive }));

    /// <summary>Reporte de eficiencia de todos los conductores (RF-06).</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(IReadOnlyList<DriverMetricsResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DriverMetricsResponse>> GetAllMetrics()
        => Ok(_metrics.GetAll());

    /// <summary>Reporte de eficiencia de un conductor (RF-06).</summary>
    [HttpGet("{id:int}/metrics")]
    [ProducesResponseType(typeof(DriverMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DriverMetricsResponse> GetMetrics(int id)
        => Ok(_metrics.GetForDriver(id));
}
