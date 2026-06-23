using System.Text.Json.Serialization;
using CourierMax.Api.Middleware;
using CourierMax.Application;
using CourierMax.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los enums viajan como texto ("Express", "Fragil") en lugar de numeros.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Las reglas de negocio se validan con FluentValidation en cada accion. El filtro
// automatico de ModelState se mantiene solo para los errores de binding/deserializacion
// (p. ej. un enum con un valor inexistente, que falla antes de que corra FluentValidation):
// sin esto, el modelo llegaria nulo a la accion y produciria un 500. Se personaliza la
// respuesta para devolver un 400 ProblemDetails uniforme y sin filtrar tipos internos.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kv => kv.Key,
                _ => new[] { "El valor proporcionado no es valido." });

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o mas errores de validacion"
        };

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CourierMax API",
        Version = "v1",
        Description = "Backbone del sistema de gestion de envios de CourierMax."
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "CourierMax.Api.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Composicion de capas.
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// El manejo de errores debe envolver todo el pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CourierMax API v1");
    options.RoutePrefix = string.Empty; // Swagger disponible en la raiz.
});

app.MapControllers();

app.Run();

// Necesario para que el proyecto de pruebas de integracion pueda referenciar el host.
public partial class Program { }
