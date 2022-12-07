using System.Collections.Generic;

namespace Bridge;

public class MessageFromClient
{
    public List<Point> Points { get; set; } = new List<Point>();
    public int NumberOfTasks { get; set; }
    public int NumberOfEpochs { get; set; }
    public int TimePhase1 { get; set; }
    public int TimePhase2 { get; set; }
    public string Mechanism { get; set; }
}
