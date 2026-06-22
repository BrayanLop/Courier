using System.Text.Json.Serialization;
using CourierMax.Api.Middleware;
using CourierMax.Application;
using CourierMax.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los enums viajan como texto ("Express", "Fragil") en lugar de numeros.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// La validacion la maneja FluentValidation, no el ModelState automatico; se
// desactiva la respuesta 400 por defecto para que el pipeline de errores sea uno solo.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
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
