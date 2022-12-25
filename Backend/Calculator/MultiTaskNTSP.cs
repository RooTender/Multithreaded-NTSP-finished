using System.Text;
using System.Text.Json;
using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiTaskNTSP
{
    private readonly IModel _channel;
    private readonly int _tasksCount;
    private readonly int _maxCycles;
    private List<Point> _bestRoute;

    public MultiTaskNTSP(int tasksCount, int firstPhaseTimeout, int secondPhaseTimeout, int maxCycles, IModel channel)
    {
        _tasksCount = tasksCount;
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

            var aboveMedianSolutions = GetSolutionsAboveMedian(routes);
            var secondPhaseSolutions = ParallelSecondPhase(aboveMedianSolutions);
            SendStatusMessage(2);

            for (var i = 0; i < _maxCycles; i++)
            {
                routes = ParallelFirstPhase(secondPhaseSolutions);
                SendStatusMessage(1);

                aboveMedianSolutions = GetSolutionsAboveMedian(routes);
                secondPhaseSolutions = ParallelSecondPhase(aboveMedianSolutions);
                SendStatusMessage(2);

                Console.WriteLine("Best distance: " + secondPhaseSolutions.Min(list => list.GetTotalDistance()));
            }
        }
        catch (OperationCanceledException) {}

        return BestRoute;
    }

    private void SendStatusMessage(int phase)
    {
        Console.WriteLine($"Status info: \n\tphase: {phase} \n\tsolution counter: {Globals.Counter}");

        var statusMessage = JsonSerializer.Serialize(new StatusInfo { Phase = phase, SolutionCounter = Globals.Counter });
        var body = Encoding.UTF8.GetBytes(statusMessage);

        _channel.BasicPublish("", Globals.Mechanism + "StatusInfo", null, body);
    }

    private List<List<Point>> GetSolutionsAboveMedian(IEnumerable<List<Point>> routes)
    {
        return routes.OrderBy(list => list.GetTotalDistance()).Take(_tasksCount).ToList();
    }

    private List<List<Point>> ParallelFirstPhase(List<Point> points)
    {
        var phase1Cts = new CancellationTokenSource();
        using var linkedCts = 
            CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase1Cts.Token);
        var tasks = new List<Task>();
        var pmxList = new List<PMX>();

        for (var i = 0; i < _tasksCount; i++)
        {
            var pmx = new PMX(points);
            pmxList.Add(pmx);

            tasks.Add(Task.Factory.StartNew(() =>
            {
                pmx.NextGeneration();
            }, linkedCts.Token));
        }

        phase1Cts.CancelAfter(FirstPhaseTimeout);

        try
        {
            Task.WaitAll(tasks.ToArray(), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 1 was canceled");
                throw;
            }
            else
            {
                Console.WriteLine("Phase 1 timeout");
            }

        }
        finally
        {
            var bestInPhase1 = pmxList.SelectMany(pmx => pmx.BestGeneration())
                .OrderBy(list => list.GetTotalDistance()).First();
            if (bestInPhase1.GetTotalDistance() < BestRoute.GetTotalDistance())
            {
                BestRoute = bestInPhase1;
            }
        }
        var routes = pmxList.SelectMany(pmx => pmx.BestGeneration()).ToList();

        return routes;

    }

    private List<List<Point>> ParallelFirstPhase(IReadOnlyCollection<List<Point>> lastGeneration)
    {
        var phase1Cts = new CancellationTokenSource();
        using var linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase1Cts.Token);
        var taskQueue = new List<Task>();
        var firstParentQueue = new Queue<List<Point>>(lastGeneration);
        var secondParentQueue =
            new Queue<List<Point>>(lastGeneration.OrderBy(_ => new Random().Next()));
        var pmxList = new List<PMX>();

        for (var i = 0; i < _tasksCount; i++)
        {
            var firstParent = new List<Point>(firstParentQueue.Dequeue());
            var secondParent = new List<Point>(secondParentQueue.Dequeue());
            var pmx = new PMX(firstParent);
            pmxList.Add(pmx);

            taskQueue.Add(Task.Factory.StartNew(() =>
            {
                pmx.NextGenerationWithParameters(firstParent, secondParent);
            }, linkedCts.Token));
        }
        phase1Cts.CancelAfter(FirstPhaseTimeout);

        try
        {
            Task.WaitAll(taskQueue.ToArray(), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 1 was canceled");
                throw;
            }
            else
            {
                Console.WriteLine("Phase 1 timeout");
            }
        }
        finally
        {
            var bestInPhase1 = pmxList.SelectMany(pmx => pmx.BestGeneration())
                .OrderBy(list => list.GetTotalDistance()).First();
            if (bestInPhase1.GetTotalDistance() < BestRoute.GetTotalDistance())
            {
                BestRoute = bestInPhase1;
            }
        }

        var routes = pmxList.SelectMany(pmx => pmx.BestGeneration()).ToList();

        return routes;
    }

    private List<List<Point>> ParallelSecondPhase(IReadOnlyList<List<Point>> aboveMedian)
    {
        var phase2Cts = new CancellationTokenSource();
        using var linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase2Cts.Token);

        var taskQueue2 = new Task[_tasksCount];
        var bestCycles = new BestCycle[_tasksCount];

        for (var i = 0; i < _tasksCount; i++)
        {
            var route = aboveMedian[i];

            var bestCycle = new BestCycle(route);
            bestCycles[i] = bestCycle;
            taskQueue2[i] = Task.Factory.StartNew(() =>
            {
                bestCycle.Find();
            }, linkedCts.Token);
        }

        phase2Cts.CancelAfter(SecondPhaseTimeout);
        try
        {
            Task.WaitAll(taskQueue2, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 2 was canceled");
                throw;
            }
            else
            {
                Console.WriteLine("Phase 2 timeout");
            }
        }
        finally
        {
            var bestInPhase2 = bestCycles.Select(bestCycle => bestCycle.GetBest())
                .OrderBy(list => list.GetTotalDistance()).First();
            if (BestRoute.GetTotalDistance() > bestInPhase2.GetTotalDistance())
            {
                BestRoute = bestInPhase2;
            }
        }
        var results = bestCycles.Select(cycle => cycle.GetBest()).ToList();


        return results;

    }

    internal class Results
    {
        public List<Point> Points { get; set; } = new();
    }
}