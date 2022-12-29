using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiThreadNTSP : ParallelNTSP
{
    public MultiThreadNTSP(int phaseCycles, int firstPhaseTimeout, int secondPhaseTimeout, int maxCycles, IModel channel)
        : base(phaseCycles, firstPhaseTimeout, secondPhaseTimeout, maxCycles, channel)
    {
    }

    protected override PMX PMXParallelMechanism(List<Point> points, CancellationToken token)
    {
        var pmx = new PMX(points);
        ThreadPool.QueueUserWorkItem(pmx.NextGeneration, token);

        return pmx;
    }

    protected override BestCycle BestCycleParallelMechanism(List<Point> points, CancellationToken token)
    {
        var pmx = new BestCycle(points);
        ThreadPool.QueueUserWorkItem(pmx.Find, token);

        return pmx;
    }

    protected override void BarrierSynchronization(CancellationToken token)
    {
        var timeout = Math.Max(FirstPhaseTimeout, SecondPhaseTimeout);

        do
        {
            ThreadPool.GetMaxThreads(out var maxThreads, out _);
            ThreadPool.GetAvailableThreads(out var idleThreads, out _);

            if (CancellationToken.IsCancellationRequested || idleThreads == maxThreads) break;

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
        } 
        while (timeout-- > 0);
    }

    protected override void Cleanup()
    {
        // Threads cleanup is handled by dotnet.
    }
}