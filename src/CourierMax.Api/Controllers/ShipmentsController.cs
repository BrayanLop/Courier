using CourierMax.Application.DTOs;
using CourierMax.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Produces("application/json")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipments;
    private readonly IValidator<CreateShipmentRequest> _createValidator;
    private readonly IValidator<AssignShipmentRequest> _assignValidator;
    private readonly IValidator<StatusChangeRequest> _statusValidator;
    private readonly IValidator<CancelShipmentRequest> _cancelValidator;

    public ShipmentsController(
        IShipmentService shipments,
        IValidator<CreateShipmentRequest> createValidator,
        IValidator<AssignShipmentRequest> assignValidator,
        IValidator<StatusChangeRequest> statusValidator,
        IValidator<CancelShipmentRequest> cancelValidator)
    {
        _shipments = shipments;
        _createValidator = createValidator;
        _assignValidator = assignValidator;
        _statusValidator = statusValidator;
        _cancelValidator = cancelValidator;
    }

    /// <summary>Registra un nuevo envio y le asigna un codigo de rastreo (RF-01).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ShipmentResponse> Create([FromBody] CreateShipmentRequest request)
    {
        _createValidator.ValidateAndThrow(request);
        var created = _shipments.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cotiza la tarifa de un envio sin registrarlo (RF-04).</summary>
    [HttpPost("quote")]
    [ProducesResponseType(typeof(TariffQuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TariffQuoteResponse> Quote([FromBody] CreateShipmentRequest request)
    {
        _createValidator.ValidateAndThrow(request);
        return Ok(_shipments.Quote(request));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShipmentResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ShipmentResponse>> GetAll()
        => Ok(_shipments.GetAll());

    /// <summary>Lista los envios atrasados creados dentro de un rango de fechas (RF-05).</summary>
    [HttpGet("delayed")]
    [ProducesResponseType(typeof(IReadOnlyList<ShipmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<ShipmentResponse>> GetDelayed(
        [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Rango invalido",
                Detail = "La fecha inicial no puede ser mayor que la final."
            });

        return Ok(_shipments.GetDelayed(from, to));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ShipmentResponse> GetById(Guid id)
        => Ok(_shipments.GetById(id));

    [HttpGet("tracking/{trackingCode}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ShipmentResponse> GetByTrackingCode(string trackingCode)
        => Ok(_shipments.GetByTrackingCode(trackingCode));

    /// <summary>Asigna el envio a un conductor/vehiculo con verificacion de capacidad (RF-03, RN-01).</summary>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<ShipmentResponse> Assign(Guid id, [FromBody] AssignShipmentRequest request)
    {
        _assignValidator.ValidateAndThrow(request);
        return Ok(_shipments.Assign(id, request));
    }

    /// <summary>Marca el envio como EN_TRANSITO (RF-02).</summary>
    [HttpPost("{id:guid}/in-transit")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<ShipmentResponse> MarkInTransit(Guid id, [FromBody] StatusChangeRequest request)
    {
        _statusValidator.ValidateAndThrow(request);
        return Ok(_shipments.MarkInTransit(id, request));
    }

    /// <summary>Marca el envio como ENTREGADO (RF-02).</summary>
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<ShipmentResponse> MarkDelivered(Guid id, [FromBody] StatusChangeRequest request)
    {
        _statusValidator.ValidateAndThrow(request);
        return Ok(_shipments.MarkDelivered(id, request));
    }

    /// <summary>Cancela el envio (RF-02, RN-03). Requiere motivo de al menos 5 caracteres.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<ShipmentResponse> Cancel(Guid id, [FromBody] CancelShipmentRequest request)
    {
        _cancelValidator.ValidateAndThrow(request);
        return Ok(_shipments.Cancel(id, request));
    }
}
