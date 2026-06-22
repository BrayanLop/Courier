namespace CourierMax.Application.Abstractions;

/// <summary>
/// Abstrae el acceso a la fecha/hora actual para que la logica dependiente del
/// tiempo (SLA, atrasos) sea deterministica y testeable.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}

/// <summary>Genera codigos de rastreo candidatos con el formato CM-XXXXXXXX (RN-05).</summary>
public interface ITrackingCodeGenerator
{
    string Generate();
}
