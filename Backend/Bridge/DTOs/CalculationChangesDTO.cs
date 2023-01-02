namespace Bridge;

public class CalculationChangesDTO
{
    public CalculationChangesDTO(bool abortCalculations, int firstPhaseTimeout, int secondPhaseTimeout)
    {
        AbortCalculations = abortCalculations;
        FirstPhaseTimeout = firstPhaseTimeout;
        SecondPhaseTimeout = secondPhaseTimeout;
    }

    public bool AbortCalculations { get; }

    public int FirstPhaseTimeout { get; }

    public int SecondPhaseTimeout { get; }
}
