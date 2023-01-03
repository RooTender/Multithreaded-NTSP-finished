using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Bridge;
using System.Data;

namespace Calculator;

public abstract class ParallelNTSP
{
    private readonly CancellationTokenSource _cancellationToken;
    private readonly IModel _channel;

    private readonly int _mechanismsEngaged;
    private readonly int _maxEpochs;
    private double _bestDistance;
    private int _currentEpoch;
    private int _firstPhaseTimeout;
    private int _secondPhaseTimeout;

    public static int CalculatedSolutions;

    protected ParallelNTSP(CalculationDTO calculationData, IModel channel)
    {
        _cancellationToken = new CancellationTokenSource();
        _channel = channel;

        _mechanismsEngaged = calculationData.MechanismsEngaged;
        _maxEpochs = calculationData.NumberOfEpochs;
        _bestDistance = calculationData.Points.GetTotalDistance();
        _firstPhaseTimeout = calculationData.PhaseOneDuration;
        _secondPhaseTimeout = calculationData.PhaseTwoDuration;
        _currentEpoch = calculationData.InitialEpoch;

        CalculatedSolutions = calculationData.CalculatedSolutions;
    }

    public void Run(List<Point> route)
    {
        SendUpdatedDistance(route);

        try
        {
            while (_currentEpoch++ <= _maxEpochs)
            {
                var routes = ParallelFirstPhase(route);
                SendStatusMessage(1);

                route = ParallelSecondPhase(routes);
                SendStatusMessage(2);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Calculations were interrupted!");
        }
    }

    public void Abort()
    {
        _cancellationToken.Cancel();
    }

    public void UpdatePhasesTimeouts(int firstPhase, int secondPhase)
    {
        _firstPhaseTimeout = firstPhase;
        _secondPhaseTimeout = secondPhase;
    }

    protected abstract PMX PMXParallelMechanism(List<Point> points, CancellationToken token);

    protected abstract ThreeOpt BestCycleParallelMechanism(List<Point> points, CancellationToken token);

    protected abstract void BarrierSynchronization(CancellationTokenSource tokenSource, int timeout);

    protected abstract void Cleanup();

    private List<List<Point>> ParallelFirstPhase(List<Point> points)
    {
        using var cancellationToken = new CancellationTokenSource();
        using var tokenLink = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken.Token, cancellationToken.Token);

        var pmxList = new List<PMX>(_mechanismsEngaged);
        for (var i = 0; i < _mechanismsEngaged; i++)
        {
            pmxList.Add(PMXParallelMechanism(points, tokenLink.Token));
        }

        cancellationToken.CancelAfter(_firstPhaseTimeout);

        try
        {
            _cancellationToken.Token.ThrowIfCancellationRequested();
            BarrierSynchronization(tokenLink, _firstPhaseTimeout);
            _cancellationToken.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 1 was canceled");
                throw;
            }

            Console.WriteLine("Phase 1 timeout");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Phase 1 timeout");
        }
        finally
        {
            Cleanup();
        }

        var results = pmxList
            .Select(x => x.BestGeneration())
            .OrderBy(x => x.GetTotalDistance()).ToList();
        
        UpdateBestDistanceValue(results.First());

        return results;
    }

    private List<Point> ParallelSecondPhase(IReadOnlyList<List<Point>> points)
    {
        using var cancellationToken = new CancellationTokenSource();
        using var tokenLink = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken.Token, cancellationToken.Token);
        
        var workers = new List<ThreeOpt>(_mechanismsEngaged);
        for (var i = 0; i < _mechanismsEngaged; i++)
        {
            workers.Add(BestCycleParallelMechanism(points[i], tokenLink.Token));
        }

        cancellationToken.CancelAfter(_secondPhaseTimeout);

        try
        {
            _cancellationToken.Token.ThrowIfCancellationRequested();
            BarrierSynchronization(tokenLink, _secondPhaseTimeout);
            _cancellationToken.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Phase 2 was canceled");
                throw;
            }

            Console.WriteLine("Phase 2 timeout");
        }
        catch (TimeoutException)
        {
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

        UpdateBestDistanceValue(bestResult);

        return bestResult;
    }

    private void UpdateBestDistanceValue(List<Point> route)
    {
        var distance = route.GetTotalDistance();

        // ReSharper disable once InvertIf
        if (distance < _bestDistance)
        {
            _bestDistance = distance;
            SendUpdatedDistance(route);
        }
    }

    private void SendUpdatedDistance(IEnumerable<Point> route)
    {
        Console.WriteLine("New best route found!");
        
        var message = JsonSerializer.Serialize(new List<Point>(route));
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish("", RabbitQueue.QueueTypes.UpdateBest, null, body);
    }

    private void SendStatusMessage(int phase)
    {
        Console.WriteLine(_currentEpoch == _maxEpochs ? "Done!" : $"Phase: {phase}, Solution: {CalculatedSolutions}");

        var message = JsonSerializer.Serialize(new CalculationStatusDTO(_currentEpoch, phase, CalculatedSolutions));
        var body = Encoding.UTF8.GetBytes(message);

        _channel.BasicPublish("", RabbitQueue.QueueTypes.Status, null, body);
    }
}