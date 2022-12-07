namespace Bridge;

public class Globals
{
    private static int _counter;
    public static int Counter { get => _counter; set { _counter = value; } }
    public static string Mechanism { get; set; } = Mechanisms.Tasks;
}
