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
        var bestCycle = new BestCycle(points);
        ThreadPool.QueueUserWorkItem(bestCycle.Find, token);

        return bestCycle;
    }

    protected override void BarrierSynchronization(CancellationTokenSource tokenSource, int timeout)
    {
        do
        {
            if (ThreadPool.PendingWorkItemCount == 0) return;

            Thread.Sleep(TimeSpan.FromMilliseconds(1));
        } 
        while (timeout-- > 0);

        tokenSource.Cancel();
        throw new TimeoutException();
    }

    protected override void Cleanup()
    {
        // Threads cleanup is handled by dotnet.
    }
}