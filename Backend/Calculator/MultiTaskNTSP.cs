using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiTaskNTSP : ParallelNTSP
{
    private readonly List<Task> _tasks;
    
    public MultiTaskNTSP(CalculationDTO calculationData, IModel channel) 
        : base(calculationData, channel)
    {
        _tasks = new List<Task>();
    }

    protected override PMX PMXParallelMechanism(List<Point> points, CancellationToken token)
    {
        var pmx = new PMX(points);
        _tasks.Add(Task.Factory.StartNew(() =>
        {
            pmx.NextGeneration();
        }, token));

        return pmx;
    }

    protected override BestCycle BestCycleParallelMechanism(List<Point> points, CancellationToken token)
    {
        var bestCycle = new BestCycle(points);
        _tasks.Add(Task.Factory.StartNew(() =>
        {
            bestCycle.Find();
        }, token));

        return bestCycle;
    }

    protected override void BarrierSynchronization(CancellationTokenSource tokenSource, int timeout)
    {
        Task.WaitAll(_tasks.ToArray(), timeout, tokenSource.Token);
    }

    protected override void Cleanup()
    {
        _tasks.Clear();
    }
}