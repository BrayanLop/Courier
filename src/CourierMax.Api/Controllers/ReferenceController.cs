using CourierMax.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

/// <summary>Expone los datos de referencia para que un cliente sepa que valores son validos.</summary>
[ApiController]
[Route("api/reference")]
[Produces("application/json")]
public sealed class ReferenceController : ControllerBase
{
    private readonly ICityCatalog _cities;
    private readonly IVehicleRepository _vehicles;

    public ReferenceController(ICityCatalog cities, IVehicleRepository vehicles)
    {
        _cities = cities;
        _vehicles = vehicles;
    }

    [HttpGet("cities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCities() => Ok(_cities.All());

    [HttpGet("vehicles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetVehicles()
        => Ok(_vehicles.GetAll().Select(v => new
        {
            v.Id,
            v.Plate,
            v.MaxWeightKg,
            v.MaxVolumeM3
        }));
}
