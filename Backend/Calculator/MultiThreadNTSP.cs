using System.Text;
using System.Text.Json;
using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiThreadNTSP
{
    public MultiThreadNTSP(int threadCount, int firstPhaseTimeout, int secondPhaseTimeout, int maxCycles, IModel channel)
    {
        _threadCount = threadCount;
        FirstPhaseTimeout = firstPhaseTimeout;
        SecondPhaseTimeout = secondPhaseTimeout;
        _maxCycles = maxCycles;
        _channel = channel;
    }

    private readonly IModel _channel;
    private readonly int _threadCount;
    private readonly int _maxCycles;
    private List<Point> _bestRoute = new();

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

    private void SendStatusMessage(int phase)
    {
        Console.WriteLine($"Status info: \n\tphase: {phase} \n\tsolution counter: {Globals.Counter}");

        var statusInfoMsg = JsonSerializer.Serialize(new StatusInfo { Phase = phase, SolutionCounter = Globals.Counter });

        var body = Encoding.UTF8.GetBytes(statusInfoMsg);

        _channel.BasicPublish("", Globals.Mechanism + "StatusInfo", null, body);
    }

    public List<Point> Run(List<Point> points)
    {
        BestRoute = new List<Point>(points);

        try
        {
            var routes = FirstParallelPhase(points);
            SendStatusMessage(1);

            var aboveMedianSolutions = GetAboveMedianSolutions(routes);
            var secondPhaseSolutions = ParallelSecondPhase(aboveMedianSolutions);
            SendStatusMessage(2);

            for (var i = 0; i < _maxCycles; i++)
            {
                routes = FirstParallelPhase(secondPhaseSolutions);
                SendStatusMessage(1);

                aboveMedianSolutions = GetAboveMedianSolutions(routes);
                secondPhaseSolutions = ParallelSecondPhase(aboveMedianSolutions);
                SendStatusMessage(2);

                Console.WriteLine("Best distance: " + secondPhaseSolutions.Min(list => list.GetTotalDistance()));
            }
        }
        catch (OperationCanceledException) {}

        return BestRoute;
    }

    private List<List<Point>> GetAboveMedianSolutions(IEnumerable<List<Point>> routes)
    {
        return routes.OrderBy(list => list.GetTotalDistance()).Take(_threadCount).ToList();
    }

	delegate void Del(object obj);

	private List<List<Point>> FirstParallelPhase(List<Point> points)
    {
        var phase1Cts = new CancellationTokenSource();
        using var linkedCts = 
            CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase1Cts.Token);


        var pmxList = new List<PMX>();

        for (var i = 0; i < _threadCount; i++)
        {
            var pmx = new PMX(points);
            pmxList.Add(pmx);

			var del = new Del(pmx.NextGeneration);
			ThreadPool.QueueUserWorkItem(new WaitCallback(del), linkedCts.Token);
        }

        phase1Cts.CancelAfter(FirstPhaseTimeout);

        try
        {
            WaitForThreads();
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

	private List<List<Point>> FirstParallelPhase(IReadOnlyCollection<List<Point>> lastGeneration)
    {
        var phase1Cts = new CancellationTokenSource();
        using var linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase1Cts.Token);

        var firstParentQueue = new Queue<List<Point>>(lastGeneration);
        var secondParentQueue =
            new Queue<List<Point>>(lastGeneration.OrderBy(t => new Random().Next()));
        var pmxList = new List<PMX>();

        for (var i = 0; i < _threadCount; i++)
        {
            var firstParent = new List<Point>(firstParentQueue.Dequeue());
            var secondParent = new List<Point>(secondParentQueue.Dequeue());
            var pmx = new PMX(firstParent, secondParent);
            pmxList.Add(pmx);

			var del = new Del(pmx.NextGeneration);
			ThreadPool.QueueUserWorkItem(new WaitCallback(del), linkedCts.Token);
        }
        phase1Cts.CancelAfter(FirstPhaseTimeout);

        try
        {
            WaitForThreads();

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

    private void WaitForThreads()
	{
		var timeOutSeconds = (int)TimeSpan.FromMilliseconds(FirstPhaseTimeout).TotalSeconds;
        
		while (timeOutSeconds > 0)
		{
            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }
			ThreadPool.GetMaxThreads(out var maxThreads, out _);
			ThreadPool.GetAvailableThreads(out var idleThreads, out _);

			if (idleThreads == maxThreads) break;

			Thread.Sleep(TimeSpan.FromMilliseconds(1000));
			--timeOutSeconds;
		}
		// You can add logic here to log timeouts
	}

	private List<List<Point>> ParallelSecondPhase(IReadOnlyList<List<Point>> aboveMedianSolutions)
    {
        var phase2Cts = new CancellationTokenSource();
        using var linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.Token, phase2Cts.Token);

        var taskQueue2 = new Task[_threadCount];
        var bestCycles = new BestCycle[_threadCount];

        for (var i = 0; i < _threadCount; i++)
        {
            var route = aboveMedianSolutions[i];

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

        return bestCycles.Select(cycle => cycle.GetBest()).ToList();
    }

    internal class Results
    {
        public List<Point> Points { get; set; } = new();
    }
}