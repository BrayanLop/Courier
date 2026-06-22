# CourierMax API

API REST para la gestion del ciclo de vida de envios de CourierMax: creacion,
asignacion a conductor/vehiculo con control de capacidad, seguimiento de estados,
calculo de tarifas, alertas de atraso (SLA) y reporte de metricas por conductor.

Construida con **.NET 8 (ASP.NET Core Web API)** siguiendo una arquitectura por
capas inspirada en Clean Architecture.

---

## Tabla de contenido

- [Requisitos](#requisitos)
- [Como ejecutar](#como-ejecutar)
- [Como correr las pruebas](#como-correr-las-pruebas)
- [Arquitectura](#arquitectura)
- [Decisiones de diseño y justificacion](#decisiones-de-diseno-y-justificacion)
- [Tecnologias](#tecnologias)
- [Modelo de dominio y reglas](#modelo-de-dominio-y-reglas)
- [Endpoints](#endpoints)
- [Ejemplos de uso (curl)](#ejemplos-de-uso-curl)
- [Datos de referencia](#datos-de-referencia)
- [Supuestos y alcance](#supuestos-y-alcance)

---

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior.

## Como ejecutar

```bash
# Desde la raiz del repositorio
dotnet restore
dotnet run --project src/CourierMax.Api
```

La consola indica el puerto (por defecto `http://localhost:5220`). La interfaz de
**Swagger UI** queda publicada en la raiz:

```
http://localhost:5220/
```

El almacenamiento es **en memoria**, de modo que los datos se reinician en cada
arranque. Los catalogos de ciudades, vehiculos y conductores se cargan con los
datos de referencia del enunciado.

## Como correr las pruebas

```bash
dotnet test
```

El proyecto `CourierMax.Tests` contiene pruebas unitarias y de flujo (41 pruebas)
que cubren tarifas, dias habiles, SLA, transiciones de estado, asignacion con
capacidad/balanceo, ciclo de vida del envio y metricas.

---

## Arquitectura

La solucion se divide en cuatro proyectos, con dependencias apuntando siempre
hacia el dominio (regla de dependencia de Clean Architecture):

```
CourierMax.Api            ->  Capa de presentacion (controllers, middleware, Swagger)
        |                     Traduce HTTP <-> casos de uso. No contiene reglas de negocio.
        v
CourierMax.Application    ->  Casos de uso, DTOs, validadores, interfaces de repositorio,
        |                     servicios de dominio (tarifas, SLA, asignacion, metricas).
        v
CourierMax.Domain         ->  Entidades, value objects, enums, excepciones y las reglas
                              de transicion del agregado Shipment. Sin dependencias externas.

CourierMax.Infrastructure ->  Implementaciones: repositorios en memoria, catalogos de
                              referencia, reloj del sistema y generador de codigos.
                              Depende de Application (implementa sus interfaces).
```

Flujo de una peticion:

```
HTTP Request
  -> Controller (valida el DTO con FluentValidation)
  -> IShipmentService / IMetricsService (orquesta el caso de uso)
  -> Servicios de dominio + agregado Shipment (aplican reglas)
  -> Repositorio (persistencia en memoria)
  -> DTO de respuesta
  ExceptionHandlingMiddleware traduce cualquier excepcion a ProblemDetails (400/404/409/500)
```

## Decisiones de diseño y justificacion

- **Almacenamiento en memoria.** El enunciado lo permite explicitamente y el foco
  esta en el diseño, no en la persistencia. Toda la persistencia se accede a traves
  de interfaces (`IShipmentRepository`, etc.), por lo que migrar a EF Core o Dapper
  implicaria solo agregar implementaciones nuevas en `Infrastructure` sin tocar la
  logica de negocio.

- **Reglas de estado dentro del agregado `Shipment`.** Las transiciones validas
  (RF-02) y la regla de cancelacion (RN-03) viven en la entidad, no en el servicio.
  Esto evita que un envio quede en un estado invalido manipulandolo desde fuera y
  mantiene la logica cohesiva (encapsulamiento, principio de responsabilidad unica).

- **Servicios de dominio enfocados.** Cada regla compleja tiene su propio servicio
  con una interfaz: `TariffCalculator` (RF-04), `BusinessDayCalculator` (RN-02),
  `SlaService` (RF-05), `AssignmentService` (RF-03/RN-01), `MetricsService` (RF-06).
  Son pequeños, testeables de forma aislada y faciles de extender.

- **Abstraccion del tiempo (`IClock`).** Toda la logica dependiente de fechas (SLA,
  atrasos, metricas) usa `IClock` en vez de `DateTime.Now`, lo que permite pruebas
  deterministas con un reloj fijo.

- **Validacion en el borde + invariantes en el dominio.** FluentValidation valida
  el formato y los rangos de entrada (RN-04) en la capa API; los value objects
  (`ContactInfo`, `PackageDetails`) protegen ademas sus invariantes basicas, de modo
  que el dominio nunca confia ciegamente en datos externos.

- **Manejo centralizado de errores.** Un unico middleware traduce las excepciones de
  dominio a respuestas `ProblemDetails` con el codigo HTTP correcto, sin filtrar
  detalles internos en los 500.

## Tecnologias

| Area | Herramienta |
|------|-------------|
| Framework | .NET 8 / ASP.NET Core Web API |
| Validacion | FluentValidation |
| Documentacion | Swagger / OpenAPI (Swashbuckle) |
| Pruebas | xUnit + FluentAssertions |
| Logging | Microsoft.Extensions.Logging (ILogger) |

---

## Modelo de dominio y reglas

| Estado | Transiciones permitidas |
|--------|--------------------------|
| `Creado` | `Asignado`, `Cancelado` |
| `Asignado` | `EnTransito`, `Cancelado` |
| `EnTransito` | `Entregado`, `Cancelado` |
| `Entregado` | (estado final) |
| `Cancelado` | (estado final) |

Cada cambio registra estado anterior y nuevo, fecha/hora, autor y motivo. El motivo
es obligatorio (minimo 5 caracteres) al cancelar.

**Calculo de tarifa (RF-04):** `total = (base + recargo_peso + recargo_distancia) * (1 + recargo_tipo)`
donde el recargo por peso es `max(0, peso - 2 kg) * 1.500`.

**SLA en dias habiles (RF-05):** Estandar = 5, Express = 2, Mismo dia = 0. Los dias
habiles excluyen sabados, domingos y festivos colombianos 2026.

**Capacidad (RN-01):** al asignar se valida que la suma de peso y volumen de los
envios activos del vehiculo mas el nuevo no supere su capacidad. Si no se indica
conductor, se elige el vehiculo con capacidad disponible y menor carga actual.

---

## Endpoints

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| `POST` | `/api/shipments` | Crear envio (RF-01) |
| `POST` | `/api/shipments/quote` | Cotizar tarifa sin registrar (RF-04) |
| `GET`  | `/api/shipments` | Listar envios |
| `GET`  | `/api/shipments/{id}` | Consultar por id |
| `GET`  | `/api/shipments/tracking/{code}` | Consultar por codigo de rastreo |
| `GET`  | `/api/shipments/delayed?from=&to=` | Envios atrasados por rango (RF-05) |
| `POST` | `/api/shipments/{id}/assign` | Asignar a conductor/vehiculo (RF-03) |
| `POST` | `/api/shipments/{id}/in-transit` | Marcar en transito (RF-02) |
| `POST` | `/api/shipments/{id}/deliver` | Marcar entregado (RF-02) |
| `POST` | `/api/shipments/{id}/cancel` | Cancelar (RF-02 / RN-03) |
| `GET`  | `/api/drivers` | Listar conductores |
| `GET`  | `/api/drivers/metrics` | Metricas de todos (RF-06) |
| `GET`  | `/api/drivers/{id}/metrics` | Metricas de un conductor (RF-06) |
| `GET`  | `/api/reference/cities` | Ciudades validas |
| `GET`  | `/api/reference/vehicles` | Vehiculos de la flota |

Codigos HTTP: `200` OK, `201` creado, `400` validacion/regla de negocio,
`404` no encontrado, `409` conflicto de estado o capacidad, `500` error interno.

---

## Ejemplos de uso (curl)

> Ajusta el puerto al que muestre la consola al arrancar (`5220` por defecto).

### Cotizar (ejemplo del enunciado: fragil 5 kg, express, Bogota -> Medellin = 40.950)

```bash
curl -X POST http://localhost:5220/api/shipments/quote \
  -H "Content-Type: application/json" \
  -d '{
    "sender":   {"name":"Ana","phone":"3001112233","address":"Calle 1"},
    "recipient":{"name":"Luis","phone":"3104445566","address":"Carrera 2"},
    "package":  {"weightKg":5,"lengthCm":30,"widthCm":30,"heightCm":30,"type":"Fragil"},
    "service":"Express","origin":"Bogota","destination":"Medellin","createdBy":"ana"
  }'
# -> {"baseRate":15000,"weightSurcharge":4500,"distanceSurcharge":12000,"typeSurcharge":9450,"total":40950}
```

### Crear un envio

```bash
curl -X POST http://localhost:5220/api/shipments \
  -H "Content-Type: application/json" \
  -d '{
    "sender":   {"name":"Ana","phone":"3001112233","address":"Calle 1"},
    "recipient":{"name":"Luis","phone":"3104445566","address":"Carrera 2"},
    "package":  {"weightKg":50,"lengthCm":40,"widthCm":40,"heightCm":40,"type":"Paquete"},
    "service":"Express","origin":"Bogota","destination":"Cali","createdBy":"ana"
  }'
```

### Asignar (automatico con balanceo de carga)

```bash
curl -X POST http://localhost:5220/api/shipments/{id}/assign \
  -H "Content-Type: application/json" \
  -d '{"changedBy":"ana"}'
```

Para asignar a un conductor especifico, incluir `"driverId": 1`.

### Avanzar estados y cancelar

```bash
curl -X POST http://localhost:5220/api/shipments/{id}/in-transit -H "Content-Type: application/json" -d '{"changedBy":"juan"}'
curl -X POST http://localhost:5220/api/shipments/{id}/deliver    -H "Content-Type: application/json" -d '{"changedBy":"juan"}'
curl -X POST http://localhost:5220/api/shipments/{id}/cancel     -H "Content-Type: application/json" -d '{"reason":"Cliente cancelo la compra","changedBy":"ana"}'
```

### Atrasados y metricas

```bash
curl "http://localhost:5220/api/shipments/delayed?from=2026-06-01&to=2026-06-30"
curl http://localhost:5220/api/drivers/1/metrics
```

Tambien se incluye una coleccion de Postman lista para importar en
[`docs/CourierMax.postman_collection.json`](docs/CourierMax.postman_collection.json).

---

## Datos de referencia

**Ciudades:** Bogota, Medellin, Cali, Barranquilla (sin tildes en la API para
evitar problemas de codificacion; la validacion no distingue mayusculas/minusculas).

**Tarifas por distancia (bidireccionales):**

| Ruta | Recargo |
|------|---------|
| Bogota - Medellin | 12.000 |
| Bogota - Cali | 9.000 |
| Bogota - Barranquilla | 20.000 |
| Medellin - Cali | 8.000 |
| Medellin - Barranquilla | 15.000 |
| Cali - Barranquilla | 18.000 |

**Flota:**

| Id | Placa | Conductor | Peso max (kg) | Volumen max (m³) |
|----|-------|-----------|---------------|------------------|
| 1 | ABC123 | Juan Perez | 500 | 10 |
| 2 | DEF456 | Maria Lopez | 300 | 6 |
| 3 | GHI789 | Carlos Ruiz | 800 | 15 |

---

## Supuestos y alcance

- **Autenticacion:** fuera de alcance para esta prueba. El "autor del cambio"
  (`changedBy` / `createdBy`) se recibe como dato en la peticion; en un sistema real
  vendria del usuario autenticado.
- **Origen y destino distintos:** un envio debe ir entre dos ciudades diferentes
  (el catalogo de distancias solo define rutas inter-ciudad).
- **Peso transportado** en las metricas cuenta los envios que estuvieron o estan en
  la via (en transito o entregados).
- **Capacidad ocupada:** un envio ocupa capacidad del vehiculo mientras esta en
  estado `Asignado` o `EnTransito`; al cancelar o entregar se libera (RN-03).
- **Codigo de rastreo:** formato `CM-` seguido de 8 digitos; la unicidad se garantiza
  reintentando contra el repositorio antes de persistir (RN-05).
- **Despliegue en nube:** no incluido (opcional/bonus segun el plazo de 3 dias).
