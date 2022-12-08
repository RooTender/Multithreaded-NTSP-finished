using System.Text;
using System.Text.Json;
using System.Threading;
using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiThreadNTSP
{
    public MultiThreadNTSP(int threadCount, int phase1TimeOut, int phase2TimeOut, int numberOfEpoch, IModel _channel)
    {
        this.threadCount = threadCount;
        this.phase1TimeOut = phase1TimeOut;
        this.phase2TimeOut = phase2TimeOut;
        NumberOfEpoch = numberOfEpoch;
        this._channel = _channel;
    }

    public CancellationTokenSource _cts = new CancellationTokenSource();
    private IModel _channel;
    private int threadCount;
    public int phase1TimeOut;
    public int phase2TimeOut;
    private int NumberOfEpoch;
    private List<Point> _bestRoute;

    public List<Point> BestRoute
    {
        get => _bestRoute;
        set
        {
            Console.WriteLine($"New best route found: {value.GetTotalDistance()}");
            
            string bestResultMsg = JsonSerializer.Serialize(new Results { points = value });

            var bestResultBody = Encoding.UTF8.GetBytes(bestResultMsg);

            _channel.BasicPublish("", Globals.Mechanism + "BestResult", null, bestResultBody);
            _bestRoute = value;
        }
    }

    private void SendStatusMessage(int phase)
    {
        Console.WriteLine($"Status info: \n\tphase: {phase} \n\tsolution counter: {Globals.Counter}");

        string statusInfoMsg = JsonSerializer.Serialize(new StatusInfo { Phase = phase, SolutionCounter = Globals.Counter });

        var body = Encoding.UTF8.GetBytes(statusInfoMsg);

        _channel.BasicPublish("", Globals.Mechanism + "StatusInfo", null, body);
    }

    public List<Point> Run(List<Point> points)
    {
        BestRoute = new List<Point>(points);

        try
        {
            var routes = ParrlerPhase1(points);
            SendStatusMessage(1);
            var aboveMediana = GetSolutionsAboveMediana(routes);
            var generetionAfterPhase2 = ParrlerPhase2(aboveMediana);
            SendStatusMessage(2);

            for (int i = 0; i < NumberOfEpoch ; i++)
            {
                routes = ParrlerPhase1(generetionAfterPhase2);
                SendStatusMessage(1);
                aboveMediana = GetSolutionsAboveMediana(routes);
                generetionAfterPhase2 = ParrlerPhase2(aboveMediana);
                SendStatusMessage(2);

                Console.WriteLine("Best distance: " + generetionAfterPhase2.Min(list => list.GetTotalDistance()));
            }
        }
        catch (OperationCanceledException e) {}

        return BestRoute;
    }

    private List<List<Point>> GetSolutionsAboveMediana(List<List<Point>> routes)
    {
        return routes.OrderBy(list => list.GetTotalDistance()).Take(threadCount).ToList();
    }

	delegate void Del(object obj);

	private List<List<Point>> ParrlerPhase1(List<Point> points)
    {
        var phase1Cts = new CancellationTokenSource();
        using CancellationTokenSource linkedCts = 
            CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, phase1Cts.Token);


        var pmxList = new List<PMX>();

        for (int i = 0; i < threadCount; i++)
        {
            var pmx = new PMX(points);
            pmxList.Add(pmx);

			var del = new Del(pmx.NextGeneration);
			ThreadPool.QueueUserWorkItem(new WaitCallback(del), linkedCts.Token);
        }

        phase1Cts.CancelAfter(phase1TimeOut);

        try
        {
            WaitForThreads();
        }
        catch (OperationCanceledException e)
        {
            if (_cts.IsCancellationRequested)
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

	private List<List<Point>> ParrlerPhase1(List<List<Point>> LastGeneration)
    {
        CancellationTokenSource phase1Cts = new CancellationTokenSource();
        using CancellationTokenSource linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, phase1Cts.Token);

        var firstParentQueue = new Queue<List<Point>>(LastGeneration);
        var secondParentQueue =
            new Queue<List<Point>>(LastGeneration.OrderBy(t => new Random().Next()));
        var pmxList = new List<PMX>();

        for (int i = 0; i < threadCount; i++)
        {
            var _firstParent = new List<Point>(firstParentQueue.Dequeue());
            var _secondParent = new List<Point>(secondParentQueue.Dequeue());
            var pmx = new PMX(_firstParent, _secondParent);
            pmxList.Add(pmx);

			var del = new Del(pmx.NextGeneration);
			ThreadPool.QueueUserWorkItem(new WaitCallback(del), linkedCts.Token);
        }
        phase1Cts.CancelAfter(phase1TimeOut);

        try
        {
            WaitForThreads();

		}
        catch (OperationCanceledException e)
        {
            if (_cts.IsCancellationRequested)
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

	protected void WaitForThreads()
	{
		int timeOutSeconds = (int)TimeSpan.FromMilliseconds(phase1TimeOut).TotalSeconds;

		//Now wait until all threads from the Threadpool have returned
		while (timeOutSeconds > 0)
		{
            if (_cts.IsCancellationRequested)
            {
                break;
            }
			//figure out what the max worker thread count it
			ThreadPool.GetMaxThreads(out int
								 maxThreads, out int placeHolder);
			ThreadPool.GetAvailableThreads(out int availThreads,
														   out placeHolder);

			if (availThreads == maxThreads) break;
			// Sleep
			Thread.Sleep(TimeSpan.FromMilliseconds(1000));
			--timeOutSeconds;
		}
		// You can add logic here to log timeouts
	}

	private List<List<Point>> ParrlerPhase2(List<List<Point>> aboveMediana)
    {
        CancellationTokenSource phase2Cts = new CancellationTokenSource();
        using CancellationTokenSource linkedCts =
               CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, phase2Cts.Token);

        var taskQueue2 = new Task[threadCount];
        BestCycle[] bestCycles = new BestCycle[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            var index = i;
            var route = aboveMediana[index];

            var bestCycle = new BestCycle(route);
            bestCycles[i] = bestCycle;
            taskQueue2[i] = Task.Factory.StartNew(() =>
            {
                bestCycle.Find();
            }, linkedCts.Token);
        }

        phase2Cts.CancelAfter(phase2TimeOut);
        try
        {
            Task.WaitAll(taskQueue2, linkedCts.Token);
        }
        catch (OperationCanceledException e)
        {
            if (_cts.IsCancellationRequested)
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
        public List<Point> points { get; set; } = new List<Point>();
    }
    
}