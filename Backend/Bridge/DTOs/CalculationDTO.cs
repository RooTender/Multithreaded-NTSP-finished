using System.Collections.Generic;

namespace Bridge;

public class CalculationDTO
{
    public CalculationDTO(
        int phaseOneDuration,
        int phaseTwoDuration,
        string mechanism,
        int mechanismsEngaged,
        int initialEpoch,
        int numberOfEpochs,
        List<Point> points,
        int calculatedSolutions
        )
    {
        PhaseOneDuration = phaseOneDuration;
        PhaseTwoDuration = phaseTwoDuration;
        Mechanism = mechanism;
        MechanismsEngaged = mechanismsEngaged;
        InitialEpoch = initialEpoch;
        NumberOfEpochs = numberOfEpochs;
        Points = points;
        CalculatedSolutions = calculatedSolutions;
    }

    public int PhaseOneDuration { get; }

    public int PhaseTwoDuration { get; }

    public string Mechanism { get; }

    public int MechanismsEngaged { get; }

    public int InitialEpoch { get; }

    public int NumberOfEpochs { get; }

    public List<Point> Points { get; }

    public int CalculatedSolutions { get; }

}