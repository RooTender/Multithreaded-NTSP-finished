using System.Text;
using System.Text.Json;
using Bridge;
using RabbitMQ.Client;

namespace Calculator;

public class MultiTaskNTSP
{
    public MultiTaskNTSP(int threadCount, int phase1TimeOut, int phase2TimeOut, int numberOfEpoch, IModel _channel)
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
    public int phase1TimeOut = 10;
    public int phase2TimeOut = 100;
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

    public MultiTaskNTSP(int threadCount)
    {
        this.threadCount = threadCount;
        NumberOfEpoch = 10;
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

    private List<List<Point>> ParrlerPhase1(List<Point> points)
    {
        CancellationTokenSource phase1Cts = new CancellationTokenSource();
        using CancellationTokenSource linkedCts = 
            CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, phase1Cts.Token);
        var TaskQueue = new List<Task>();
        var pmxList = new List<PMX>();

        for (int i = 0; i < threadCount; i++)
        {
            var pmx = new PMX(points);
            pmxList.Add(pmx);

            TaskQueue.Add(Task.Factory.StartNew(() =>
            {
                pmx.NextGeneration();
            }, linkedCts.Token));
        }

        phase1Cts.CancelAfter(phase1TimeOut);

        try
        {
            Task.WaitAll(TaskQueue.ToArray(), linkedCts.Token);
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
        var taskQueue = new List<Task>();
        var firstParentQueue = new Queue<List<Point>>(LastGeneration);
        var secondParentQueue =
            new Queue<List<Point>>(LastGeneration.OrderBy(t => new Random().Next()));
        var pmxList = new List<PMX>();

        for (int i = 0; i < threadCount; i++)
        {
            var _firstParent = new List<Point>(firstParentQueue.Dequeue());
            var _secondParent = new List<Point>(secondParentQueue.Dequeue());
            var pmx = new PMX(_firstParent);
            pmxList.Add(pmx);

            taskQueue.Add(Task.Factory.StartNew(() =>
            {
                pmx.NextGeneration(_firstParent, _secondParent);
            }, linkedCts.Token));
        }
        phase1Cts.CancelAfter(phase1TimeOut);

        try
        {
            Task.WaitAll(taskQueue.ToArray(), linkedCts.Token);
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