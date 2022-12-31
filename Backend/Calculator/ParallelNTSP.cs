using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Bridge;

namespace Calculator;

public abstract class ParallelNTSP
{
    private readonly IModel _channel;
    private readonly int _phaseCycles;
    private readonly int _maxCycles;
    private List<Point> _bestRoute;

    protected ParallelNTSP(int phaseCycles, int firstPhaseTimeout, int secondPhaseTimeout, int maxCycles, IModel channel)
    {
        _phaseCycles = phaseCycles;
        _maxCycles = maxCycles;
        _channel = channel;
        _bestRoute = new List<Point>();

        FirstPhaseTimeout = firstPhaseTimeout;
        SecondPhaseTimeout = secondPhaseTimeout;
    }

    public CancellationTokenSource CancellationToken = new();
    public int FirstPhaseTimeout;
    public int SecondPhaseTimeout;
    public List<Point> BestRoute
    {
        get => _bestRoute;
        set
        {
            Console.WriteLine($"New best route found: {value.GetTotalDistance()}");
                
            var bestResultMsg = JsonSerializer.Serialize(new Results { Points = value });
            var bestResultBody = Encoding.UTF8.GetBytes(bestResultMsg);

            _channel.BasicPublish("", Globals.Mechanism + "BestResult", null, bestResultBody);
            _bestRoute = value;
        }
    }

    public List<Point> Run(List<Point> points)
    {
        BestRoute = new List<Point>(points);

        try
        {
            var routes = ParallelFirstPhase(points);
            SendStatusMessage(1);

            var secondPhaseSolutions = ParallelSecondPhase(routes);
            SendStatusMessage(2);

            for (var i = 0; i < _maxCycles; i++)
            {
                routes = ParallelFirstPhase(secondPhaseSolutions);
                SendStatusMessage(1);

                secondPhaseSolutions = ParallelSecondPhase(routes);
                SendStatusMessage(2);

                Console.WriteLine("Best distance: " + secondPhaseSolutions.GetTotalDistance());
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Calculations were interrupted!");
        }

        return BestRoute;
    }

    protected abstract PMX PMXParallelMechanism(List<Point> points, CancellationToken token);

    protected abstract BestCycle BestCycleParallelMechanism(List<Point> points, CancellationToken token);

    protected abstract void BarrierSynchronization(CancellationToken token);

    protected abstract void Cleanup();

    private List<List<Point>> ParallelFirstPhase(List<Point> points)
    {
        using var cancellationToken = new CancellationTokenSource();
        using var tokenLink = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, cancellationToken.Token);

        var pmxList = new List<PMX>(_phaseCycles);
        for (var i = 0; i < _phaseCycles; i++)
        {
            pmxList.Add(PMXParallelMechanism(points, tokenLink.Token));
        }

        cancellationToken.CancelAfter(FirstPhaseTimeout);

        try
        {
            BarrierSynchronization(tokenLink.Token);
        }
        catch (OperationCanceledException)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 1 was canceled");
                throw;
            }

            Console.WriteLine("Phase 1 timeout");
        }
        finally
        {
            Cleanup();
        }

        var results = pmxList
            .Select(x => x.BestGeneration())
            .OrderBy(x => x.GetTotalDistance()).ToList();

        var bestResult = results.First();
        if (bestResult.GetTotalDistance() < BestRoute.GetTotalDistance())
        {
            BestRoute = bestResult;
        }

        return results;
    }

    private List<Point> ParallelSecondPhase(IReadOnlyList<List<Point>> points)
    {
        using var cancellationToken = new CancellationTokenSource();
        using var tokenLink = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, cancellationToken.Token);
        
        var workers = new List<BestCycle>(_phaseCycles);
        for (var i = 0; i < _phaseCycles; i++)
        {
            workers.Add(BestCycleParallelMechanism(points[i], tokenLink.Token));
        }

        cancellationToken.CancelAfter(SecondPhaseTimeout);
        try
        {
            BarrierSynchronization(tokenLink.Token);
        }
        catch (OperationCanceledException)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 2 was canceled");
                throw;
            }

            Console.WriteLine("Phase 2 timeout");
        }
        finally
        {
            Cleanup();
        }

        var bestResult = workers
            .Select(x => x.GetBest())
            .OrderBy(x => x.GetTotalDistance())
            .First();

        if (BestRoute.GetTotalDistance() > bestResult.GetTotalDistance())
        {
            BestRoute = bestResult;
        }

        return bestResult;
    }

    private void SendStatusMessage(int phase)
    {
        Console.WriteLine($"Status info: \n\tphase: {phase} \n\tsolution counter: {Globals.Counter}");

        var statusMessage = JsonSerializer.Serialize(new StatusInfo { Phase = phase, SolutionCounter = Globals.Counter });
        var body = Encoding.UTF8.GetBytes(statusMessage);

        _channel.BasicPublish("", Globals.Mechanism + "StatusInfo", null, body);
    }

    internal class Results
    {
        public List<Point> Points { get; set; } = new();
    }
}