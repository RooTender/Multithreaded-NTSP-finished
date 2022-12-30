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
using Bridge;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Calculator;
using Point = Bridge.Point;

namespace GUI.ViewModels;

public delegate void UpdateStatusEventHandler(StatusInfo results);
public delegate void UpdateBestResultEventHandler(Results results);

internal static class DefaultValues
{
    internal static readonly int PhaseDuration = 2;
    internal static readonly int QuantityForMechanism = 8;
    internal static readonly int NumberOfEpochs = 50;  // mayby add to front
    internal static readonly char MeasureUnit = TimeManager.Units.Seconds;
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
    private int _currentEpoch;

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    public int QuantityForMechanism
    {
        get => _quantityForMechanism;
        set
        {
            _quantityForMechanism = value;
            OnPropertyChanged();
        }
    }

    public int PhaseOneDuration
    {
        get => _phaseOneDuration;
        set
        {
            _phaseOneDuration = value;
            OnPropertyChanged();
        }
    }
    public int PhaseTwoDuration
    {
        get => _phaseTwoDuration;
        set
        {
            _phaseTwoDuration = value;
            OnPropertyChanged();
        }
    }

    public char PhaseOneMeasureUnit
    {
        get => _phaseOneMeasureUnit;
        set
        {
            _phaseOneMeasureUnit = value;
            OnPropertyChanged();
        }
    }
    public char PhaseTwoMeasureUnit
    {
        get => _phaseTwoMeasureUnit;
        set
        {
            _phaseTwoMeasureUnit = value;
            OnPropertyChanged();
        }
    }

    public int PhaseOneDurationInMs
    {
        get => _phaseOneDurationInMs;
        set
        {
            _phaseOneDurationInMs = value;
            OnPropertyChanged();
        }
    }
    public int PhaseTwoDurationInMs
    {
        get => _phaseTwoDurationInMs;
        set
        {
            _phaseTwoDurationInMs = value;
            OnPropertyChanged();
        }
    }

    public int NumberOfEpochs
    {
        get => _numberOfEpochs;
        set
        {
            _numberOfEpochs = value;
            OnPropertyChanged();
        }
    }

    public string BestResult
    {
        get => _bestResult;
        set
        {
            _bestResult = value;
            OnPropertyChanged();
        }
    }
    public string SolutionCount
    {
        get => _solutionCount;
        set
        {
            _solutionCount = value;
            OnPropertyChanged();
        }
    }
    public string CalculationStatus
    {
        get => _calculationStatus;
        set
        {
            _calculationStatus = value;
            OnPropertyChanged();
        }
    }

    public bool Start
    {
        get => _start;
        set
        {
            _start = value;
            OnPropertyChanged();
        }
    }
    public bool Stop
    {
        get => _stop;
        set
        {
            _stop = value;
            OnPropertyChanged();
        }
    }

    public int CurrentEpoch
    {
        get => _currentEpoch;
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

	public MainWindow MainWindow { get; }
	public ICommand OpenFileCommand { get; set; }
    public ICommand ChooseQuantityForMechanismCommand { get; set; }
    public ICommand ChoosePhaseOneDurationCommand { get; set; }
    public ICommand ChoosePhaseTwoDurationCommand { get; set; }    
    public ICommand ChoosePhaseOneMeasureUnitCommand { get; set; }
    public ICommand ChoosePhaseTwoMeasureUnitCommand { get; set; }
    public ICommand ChooseNumberOfEpochsCommand { get; set; }
    public ICommand StartCommand { get; set; }
    public ICommand StopCommand { get; set; }
    public ICommand UpdateCommand { get; set; }
    public ICommand ContinueCommand { get; set; }
    public List<Point>? Points { get; private set; }
    public List<Node> OrderedNodes { get; private set; }

    #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainViewModel(MainWindow mainWindow)
    {
        MainWindow = mainWindow;

        OpenFileCommand = new RelayCommand(OpenFile);
        ChooseQuantityForMechanismCommand = new RelayCommand(ChooseQuantityForMechanism);
        ChoosePhaseOneDurationCommand = new RelayCommand(ChoosePhaseOneDuration);
        ChoosePhaseTwoDurationCommand = new RelayCommand(ChoosePhaseTwoDuration);
        ChoosePhaseOneMeasureUnitCommand = new RelayCommand(ChoosePhaseOneMeasureUnit);
        ChoosePhaseTwoMeasureUnitCommand = new RelayCommand(ChoosePhaseTwoMeasureUnit);
        ChooseNumberOfEpochsCommand = new RelayCommand(ChooseNumberOfEpochs);
        StartCommand = new RelayCommand(StartCalculations);
        StopCommand = new RelayCommand(StopCalculations);
        UpdateCommand = new RelayCommand(UpdateCalculations);
		ContinueCommand = new RelayCommand(ContinueCalculations);

        ReceivedStatusUpdate += UpdateStatus;
        ReceivedBestResult += UpdateBestResult;
    }

	private void ContinueCalculations(object obj)
	{
		Start = true;
		Stop = false;

		PhaseOneDurationInMs = TimeManager.GetDurationInMs(PhaseOneDuration, PhaseOneMeasureUnit);
		PhaseTwoDurationInMs = TimeManager.GetDurationInMs(PhaseOneDuration, PhaseOneMeasureUnit);

		MessagingHelper.SendInitialMessageFromClient(
			PhaseOneDurationInMs,
			PhaseTwoDurationInMs,
			QuantityForMechanism,
			NumberOfEpochs - CurrentEpoch,
            Points,
			MainWindow.Mechanism.Text);

		MainWindow.EnableWindowClosing();
	}

	// TODO add : https://learn.microsoft.com/en-us/answers/questions/746124/how-to-disable-and-enable-window-close-button-x-in.html
	private void UpdateCalculations(object obj)
    {
        EditCalculations();
    }

    private void ChooseNumberOfEpochs(object obj)
    {
        var integerUpDown = obj as Xceed.Wpf.Toolkit.IntegerUpDown;
        NumberOfEpochs = integerUpDown?.Value ?? DefaultValues.PhaseDuration;
    }

    private void UpdateBestResult(Results results)
    {
        Points = results.Points;
        BestResult = Math.Round(Points.GetTotalDistance(), 2).ToString(CultureInfo.InvariantCulture);
        DrawGraph(Points);
        UpdateNodesDataGrid(Points);
    }
    private static int _helperCounter;
    private static bool _endOfRun;
    private void UpdateStatus(StatusInfo statusInfo)
    {
        if (_endOfRun) return;

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
            _endOfRun = true;
            MainWindow.DisableWindowClosing();
        }
        else
        {
            CalculationStatus = $"Phase: {statusInfo.Phase}";
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
        var integerUpDown = obj as TextBox;
        PhaseOneDuration = int.TryParse(integerUpDown?.Text, out var result) ? result : 0;
    }

    private void ChoosePhaseTwoDuration(object obj)
    {
        var integerUpDown = obj as TextBox;
        PhaseTwoDuration = int.TryParse(integerUpDown?.Text, out var result) ? result : 0;
    }

    private void StopCalculations(object obj)
    {
        Stop = true;
        Start = false;
	}

    private void StartCalculations(object obj)
    {
		MessagingHelper.Setup(ReceivedStatusUpdate, ReceivedBestResult, MainWindow.Mechanism.Text);
		CurrentEpoch = 0;
        _endOfRun = false;
        Start = true;
        Stop = false;

        PhaseOneDurationInMs = TimeManager.GetDurationInMs(PhaseOneDuration, PhaseOneMeasureUnit);
        PhaseTwoDurationInMs = TimeManager.GetDurationInMs(PhaseTwoDuration, PhaseTwoMeasureUnit);

        if (Points == null)
        {
            MessageBox.Show(
                "Please load the points to analyze!", 
                "No points were loaded", 
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        MessagingHelper.SendInitialMessageFromClient(
            PhaseOneDurationInMs,
            PhaseTwoDurationInMs,
            QuantityForMechanism,
            NumberOfEpochs,
            Points,
            MainWindow.Mechanism.Text);

		MainWindow.EnableWindowClosing();
		MessagingHelper.ReceiveMessages();
    }

    private void EditCalculations()
    {
        if (Start)
        {
			MainWindow.EnableWindowClosing();
		}
        else
        {
			MainWindow.DisableWindowClosing();
		}

		MessagingHelper.SendEditMessage(Stop, PhaseOneDurationInMs, PhaseTwoDurationInMs);
    }

    private void ChoosePhaseOneMeasureUnit(object obj)
    {
        var el = obj as ComboBox;
        PhaseOneMeasureUnit = el?.Text.ElementAt(0) ?? 's';
    }

    private void ChoosePhaseTwoMeasureUnit(object obj)
    {
        var el = obj as ComboBox;
        PhaseTwoMeasureUnit = el?.Text.ElementAt(0) ?? 's';
    }

    private void ChooseQuantityForMechanism(object obj)
    {
        var integerUpDown = obj as TextBox;
        QuantityForMechanism = int.TryParse(integerUpDown?.Text, out var result) ? result : 0;
    }

    private void OpenFile(object obj)
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == false) return;

        FileName = openFileDialog.FileName;
        Points = FileManager.ReadPoints(FileName);
        OrderedNodes = Points.Select((p, id) => new Node(id, p)).ToList();
        
        DrawGraph(Points);
        UpdateNodesDataGrid(Points);
    }

    private void UpdateNodesDataGrid(IEnumerable<Point> points)
    {
        Nodes = new ObservableCollection<Node>(GetNodeIdsForPoints(points));
    }

    private IEnumerable<Node> GetNodeIdsForPoints(IEnumerable<Point> points)
    {
        return points.Select(point => OrderedNodes.First(thisPoint => 
            thisPoint.X - point.X == 0 && 
            thisPoint.Y - point.Y == 0));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
