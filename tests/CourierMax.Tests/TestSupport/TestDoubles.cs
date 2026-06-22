using CourierMax.Application.Abstractions;

namespace CourierMax.Tests.TestSupport;

/// <summary>Reloj controlable para hacer deterministas las pruebas dependientes del tiempo.</summary>
public sealed class TestClock : IClock
{
    public TestClock(DateTime now) => Now = now;
    public DateTime Now { get; set; }
}

/// <summary>
/// Generador de codigos predecible. Devuelve los codigos encolados en orden; si
/// se agota la cola, produce codigos secuenciales validos.
/// </summary>
public sealed class StubTrackingCodeGenerator : ITrackingCodeGenerator
{
    private readonly Queue<string> _queued;
    private int _counter;

    public StubTrackingCodeGenerator(params string[] codes)
    {
        _queued = new Queue<string>(codes);
    }

    public string Generate()
        => _queued.Count > 0 ? _queued.Dequeue() : $"CM-{++_counter:D8}";
}
