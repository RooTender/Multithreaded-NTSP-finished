namespace Bridge;

public class CalculationStatusDTO
{
    public CalculationStatusDTO(int epoch, int phase, int solutionNo)
    {
        Epoch = epoch;
        Phase = phase;
        SolutionNo = solutionNo;
    }

    public int Epoch { get; }

    public int Phase { get; }

    public int SolutionNo { get; }
}
