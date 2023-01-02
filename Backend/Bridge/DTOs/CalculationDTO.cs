using System.Collections.Generic;

namespace Bridge;

public class CalculationDTO
{
    public CalculationDTO(
        int phaseOneDurationInMs,
        int phaseTwoDurationInMs,
        int mechanismsEngaged,
        int numberOfEpochs,
        List<Point> points,
        string mechanism)
    {
        PhaseOneDurationInMs = phaseOneDurationInMs;
        PhaseTwoDurationInMs = phaseTwoDurationInMs;
        MechanismsEngaged = mechanismsEngaged;
        NumberOfEpochs = numberOfEpochs;
        Points = points;
        Mechanism = mechanism;
    }

    public int PhaseOneDurationInMs { get; }

    public int PhaseTwoDurationInMs { get; }

    public int MechanismsEngaged { get; }

    public int NumberOfEpochs { get; }

    public List<Point> Points { get; }

    public string Mechanism { get; }
}