namespace Bridge;

public class Mechanism
{
    public static readonly string[] Types = { Type.Tasks, Type.Threads };

    public static class Type
    {
        public const string Tasks = "Tasks";
        public const string Threads = "Threads";
    }
}