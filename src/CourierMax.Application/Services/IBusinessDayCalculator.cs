namespace CourierMax.Application.Services;

public interface IBusinessDayCalculator
{
    bool IsBusinessDay(DateTime date);

    /// <summary>Suma una cantidad de dias habiles a una fecha, saltando fines de semana y festivos.</summary>
    DateTime AddBusinessDays(DateTime start, int businessDays);

    /// <summary>Cuenta los dias habiles transcurridos entre dos fechas (excluye la fecha inicial).</summary>
    int BusinessDaysBetween(DateTime start, DateTime end);
}
