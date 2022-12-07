using Microsoft.Win32;
using OxyPlot.Series;
using OxyPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GUI.Utils;
using System.Collections.ObjectModel;
using GUI.Model;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using Bridge;
using System;
using Calculator;
using System.Threading;

namespace GUI.ViewModels;

public delegate void UpdateStatusEventHandler(StatusInfo results);
public delegate void UpdateBestResultEventHandler(Results results);

internal static class DefaultValues
{
    internal readonly static int PhaseDuration = 2;
    internal readonly static int QuantityForMechanism = 8;
    internal readonly static int NumberOfEpochs = 50;  // mayby add to front
    internal readonly static char MeasureUnit = TimeManager.Units.Seconds;
}

public class MainViewModel : INotifyPropertyChanged
{
    #region Fields

    private string _fileName;
    private string _mechanism = Mechanisms.Tasks;
    private int _quantityForMechanism = DefaultValues.QuantityForMechanism;
    private int _phaseOneDuration = DefaultValues.PhaseDuration;
    private int _phaseTwoDuration = DefaultValues.PhaseDuration;
    private char _phaseOneMeasureUnit = DefaultValues.MeasureUnit;
    private char _phaseTwoMeasureUnit = DefaultValues.MeasureUnit;
    private int _phaseOneDurationInMs = DefaultValues.PhaseDuration;
    private int _phaseTwoDurationInMs = DefaultValues.PhaseDuration;

    private int _numberOfEpochs = DefaultValues.NumberOfEpochs;

    private string _bestResult;
    private string _solutionCount;
    private string _calculationStatus;
    private bool _start;
    private bool _stop;
    private PlotModel _plotModel;
    private ObservableCollection<Node> _nodes;
    private int _currentEpoch = 0;

    public string FileName
    {
        get { return _fileName; }
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    public string Mechanism
    {
        get { return _mechanism; }
        set
        {
            _mechanism = value;
            OnPropertyChanged();
        }
    }

    public int QuantityForMechanism
    {
        get { return _quantityForMechanism; }
        set
        {
            _quantityForMechanism = value;
            OnPropertyChanged();
        }
    }

    public int PhaseOneDuration
    {
        get { return _phaseOneDuration; }
        set
        {
            _phaseOneDuration = value;
            OnPropertyChanged();
        }
    }
    public int PhaseTwoDuration
    {
        get { return _phaseTwoDuration; }
        set
        {
            _phaseTwoDuration = value;
            OnPropertyChanged();
        }
    }

    public char PhaseOneMeasureUnit
    {
        get { return _phaseOneMeasureUnit; }
        set
        {
            _phaseOneMeasureUnit = value;
            OnPropertyChanged();
        }
    }
    public char PhaseTwoMeasureUnit
    {
        get { return _phaseTwoMeasureUnit; }
        set
        {
            _phaseTwoMeasureUnit = value;
            OnPropertyChanged();
        }
    }

    public int PhaseOneDurationInMs
    {
        get { return _phaseOneDurationInMs; }
        set
        {
            _phaseOneDurationInMs = value;
            OnPropertyChanged();
        }
    }
    public int PhaseTwoDurationInMs
    {
        get { return _phaseTwoDurationInMs; }
        set
        {
            _phaseTwoDurationInMs = value;
            OnPropertyChanged();
        }
    }

    public int NumberOfEpochs
    {
        get { return _numberOfEpochs; }
        set
        {
            _numberOfEpochs = value;
            OnPropertyChanged();
        }
    }

    public string BestResult
    {
        get { return _bestResult; }
        set
        {
            _bestResult = value;
            OnPropertyChanged();
        }
    }
    public string SolutionCount
    {
        get { return _solutionCount; }
        set
        {
            _solutionCount = value;
            OnPropertyChanged();
        }
    }
    public string CalculationStatus
    {
        get { return _calculationStatus; }
        set
        {
            _calculationStatus = value;
            OnPropertyChanged();
        }
    }

    public bool Start
    {
        get { return _start; }
        set
        {
            _start = value;
            OnPropertyChanged();
        }
    }
    public bool Stop
    {
        get { return _stop; }
        set
        {
            _stop = value;
            OnPropertyChanged();
        }
    }

    public int CurrentEpoch
    {
        get { return _currentEpoch; }
        set
        {
            _currentEpoch = value;
            OnPropertyChanged();
        }
    }

    public PlotModel PlotModel
    {
        get => _plotModel;
        set
        {
            _plotModel = value;
            OnPropertyChanged();
        }
    }
    public ObservableCollection<Node> Nodes
    {
        get => _nodes;
        set
        {
            _nodes = value;
            OnPropertyChanged();
        }
    }

    public static string Tasks { get; set; } = Mechanisms.Tasks.ToString();
    public static string Processes { get; set; } = Mechanisms.Processes.ToString();

    public event PropertyChangedEventHandler? PropertyChanged;

    public event UpdateStatusEventHandler ReceivedStatusUpdate;
    public event UpdateBestResultEventHandler ReceivedBestResult;

    public ICommand OpenFileCommand { get; set; }
    public ICommand ChooseMechanismCommand { get; set; }
    public ICommand ChooseQuantityForMechanismCommand { get; set; }
    public ICommand ChoosePhaseOneDurationCommand { get; set; }
    public ICommand ChoosePhaseTwoDurationCommand { get; set; }    
    public ICommand ChoosePhaseOneMeasureUnitCommand { get; set; }
    public ICommand ChoosePhaseTwoMeasureUnitCommand { get; set; }
    public ICommand ChooseNumberOfEpochsCommand { get; set; }
    public ICommand StartCommand { get; set; }
    public ICommand StopCommand { get; set; }
    public List<Point> Points { get; private set; }
    public List<Node> OrderdNodes { get; private set; }

    #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(OpenFile);
        ChooseMechanismCommand = new RelayCommand(ChooseMechanism);
        ChooseQuantityForMechanismCommand = new RelayCommand(ChooseQuantityForMechanism);
        ChoosePhaseOneDurationCommand = new RelayCommand(ChoosePhaseOneDuration);
        ChoosePhaseTwoDurationCommand = new RelayCommand(ChoosePhaseTwoDuration);
        ChoosePhaseOneMeasureUnitCommand = new RelayCommand(ChoosePhaseOneMeasureUnit);
        ChoosePhaseTwoMeasureUnitCommand = new RelayCommand(ChoosePhaseTwoMeasureUnit);
        ChooseNumberOfEpochsCommand = new RelayCommand(ChooseNumberOfEpochs);
        StartCommand = new RelayCommand(StartCalculations);
        StopCommand = new RelayCommand(StopCalculations);

        ReceivedStatusUpdate += new UpdateStatusEventHandler(UpdateStatus);
        ReceivedBestResult += new UpdateBestResultEventHandler(UpdateBestResult);

        MessagingHelper messagingHelper = new(Mechanism);
        MessagingHelper.Setup(ReceivedStatusUpdate, ReceivedBestResult);
    }

    private void ChooseNumberOfEpochs(object obj)
    {
        var integerUpDown = obj as Xceed.Wpf.Toolkit.IntegerUpDown;
        NumberOfEpochs = integerUpDown?.Value ?? DefaultValues.PhaseDuration;
    }

    private void UpdateBestResult(Results results)
    {
        Points = results.points;
        BestResult = Math.Round(Points.GetTotalDistance(), 2).ToString();
        DrawGraph(Points);
        UpdateNodesDataGrid(Points);
    }
    private static int _helperCounter = 0;
    private static bool endOfRun = false;
    private void UpdateStatus(StatusInfo statusInfo)
    {
        if (!endOfRun)
        {
            _helperCounter++;
            if (_helperCounter == 2)
            {
                CurrentEpoch++;
                _helperCounter = 0;
            }

            SolutionCount = statusInfo.SolutionCounter.ToString();

            if (CurrentEpoch >= DefaultValues.NumberOfEpochs)
            {
                CalculationStatus = "Ready!";               
                endOfRun = true;
            }
            else
            {
                CalculationStatus = $"Phase: {statusInfo.Phase}";
            }
        }
    }

    private void DrawGraph(List<Point> points)
    {
        var plotModel = new PlotModel { Title = "Actual solution graph" };
        var series = new LineSeries { MarkerType = MarkerType.Circle };
        points = AddFirstPointToTheEnd(points);
        var dataPoints = points.Select(p => new DataPoint(p.X,p.Y));
        series.Points.AddRange(dataPoints);
        plotModel.Series.Add(series);
        PlotModel = plotModel;
    }

    private List<Point> AddFirstPointToTheEnd(List<Point> points)
    {
        points.Add(points[0]);
        return points;
    }

    private void ChoosePhaseOneDuration(object obj)
    {
        var integerUpDown = obj as Xceed.Wpf.Toolkit.IntegerUpDown;
        PhaseOneDuration = integerUpDown?.Value ?? DefaultValues.PhaseDuration;
    }

    private void ChoosePhaseTwoDuration(object obj)
    {
        var integerUpDown = obj as Xceed.Wpf.Toolkit.IntegerUpDown;
        PhaseTwoDuration = integerUpDown?.Value ?? DefaultValues.PhaseDuration;
    }

    private void StopCalculations(object obj)
    {
        Stop = true;
        Start = false;
        EditCalculations();
    }

    private void StartCalculations(object obj)
    {
        CurrentEpoch = 0;
        endOfRun = false;
        Start = true;
        Stop = false;

        PhaseOneDurationInMs = TimeManager.GetDurationInMs(PhaseOneDuration, PhaseOneMeasureUnit);
        PhaseTwoDurationInMs = TimeManager.GetDurationInMs(PhaseOneDuration, PhaseOneMeasureUnit);

        MessagingHelper.SendInitialMessageFromClient(
            PhaseOneDurationInMs,
            PhaseTwoDurationInMs,
            QuantityForMechanism,
            NumberOfEpochs,
            Points,
            Mechanism);

        MessagingHelper.ReceiveMessages();
    }

    private void EditCalculations()
    {
        MessagingHelper.SendEditMessage(Stop, PhaseOneDurationInMs, PhaseTwoDurationInMs);
    }

    private void ChoosePhaseOneMeasureUnit(object obj)
    {
        var el = obj as System.Windows.Controls.ComboBox;
        PhaseOneMeasureUnit = el?.Text.ElementAt(0) ?? 's';
    }

    private void ChoosePhaseTwoMeasureUnit(object obj)
    {
        var el = obj as System.Windows.Controls.ComboBox;
        PhaseTwoMeasureUnit = el?.Text.ElementAt(0) ?? 's';
    }

    private void ChooseQuantityForMechanism(object obj)
    {
        var el = obj as Xceed.Wpf.Toolkit.IntegerUpDown;
        QuantityForMechanism = el?.Value ?? DefaultValues.QuantityForMechanism;
    }

    private void ChooseMechanism(object mechanismName)
    {
        Mechanism = (string)mechanismName;
    }

    private void OpenFile(object obj)
    {
        var openFileDialog = new OpenFileDialog();

        if (openFileDialog.ShowDialog() == true)
        {
            FileName = openFileDialog.FileName;
            Points = FileManager.ReadPoints(FileName);
            OrderdNodes = Points.Select((p, id) => new Node(id, p)).ToList();

            // graph
            DrawGraph(Points);

            // table
            UpdateNodesDataGrid(Points);
        }
    }

    private void UpdateNodesDataGrid(List<Point> points)
    {
        var nodes = GetNodeIdsForPoints(points);
        Nodes = new ObservableCollection<Node>(nodes);
    }

    private IEnumerable<Node> GetNodeIdsForPoints(List<Point> points)
    {
        foreach (var point in points)
        {
            yield return OrderdNodes.First(n => n.X == point.X && n.Y == point.Y);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
