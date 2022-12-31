﻿using Microsoft.Win32;
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
using System.Windows.Controls.Primitives;
using Calculator;
using Point = Bridge.Point;

namespace GUI.ViewModels;

public delegate void UpdateStatusEventHandler(StatusInfo results);
public delegate void UpdateBestResultEventHandler(Results results);

internal static class DefaultValues
{
    internal static readonly int PhaseDuration = 2;
    internal static readonly int QuantityForMechanism = 8;
    internal static readonly int NumberOfEpochs = 50;
}

internal static class MeasurementUnits
{
    internal const string Milliseconds = "ms";
    internal const string Seconds = "s";
}

public class MainViewModel : INotifyPropertyChanged
{
    #region Fields

    private string _fileName;
    private int _quantityForMechanism = DefaultValues.QuantityForMechanism;
    private int _phaseOneDuration = DefaultValues.PhaseDuration;
    private int _phaseTwoDuration = DefaultValues.PhaseDuration;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public event UpdateStatusEventHandler ReceivedStatusUpdate;
    public event UpdateBestResultEventHandler ReceivedBestResult;

	public MainWindow MainWindow { get; }
	public ICommand OpenFileCommand { get; set; }
    public ICommand ChooseQuantityForMechanismCommand { get; set; }
    public ICommand ChoosePhaseOneDurationCommand { get; set; }
    public ICommand ChoosePhaseTwoDurationCommand { get; set; }
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

        SetupUnitMeasurementComboBox(mainWindow.PhaseOneMeasureUnit);
        SetupUnitMeasurementComboBox(mainWindow.PhaseTwoMeasureUnit);

        OpenFileCommand = new RelayCommand(OpenFile);
        ChooseQuantityForMechanismCommand = new RelayCommand(ChooseQuantityForMechanism);
        ChoosePhaseOneDurationCommand = new RelayCommand(ChoosePhaseOneDuration);
        ChoosePhaseTwoDurationCommand = new RelayCommand(ChoosePhaseTwoDuration);
        ChooseNumberOfEpochsCommand = new RelayCommand(ChooseNumberOfEpochs);
        StartCommand = new RelayCommand(StartCalculations);
        StopCommand = new RelayCommand(StopCalculations);
        UpdateCommand = new RelayCommand(UpdateCalculations);
		ContinueCommand = new RelayCommand(ContinueCalculations);

        ReceivedStatusUpdate += UpdateStatus;
        ReceivedBestResult += UpdateBestResult;
    }

    private static void SetupUnitMeasurementComboBox(Selector comboBox)
    {
        comboBox.Items.Add(MeasurementUnits.Milliseconds);
        comboBox.Items.Add(MeasurementUnits.Seconds);

        comboBox.SelectedIndex = 0;
    }

    private void StartCalculations(object obj)
    {
        MessagingHelper.Setup(ReceivedStatusUpdate, ReceivedBestResult, MainWindow.Mechanism.Text);
        CurrentEpoch = 0;
        _endOfRun = false;
        Start = true;
        Stop = false;

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
            NormalizeDuration(PhaseOneDuration, MainWindow.PhaseOneMeasureUnit.Text),
            NormalizeDuration(PhaseTwoDuration, MainWindow.PhaseTwoMeasureUnit.Text),
            QuantityForMechanism,
            NumberOfEpochs,
            Points,
            MainWindow.Mechanism.Text);

        MainWindow.EnableWindowClosing();
        MessagingHelper.ReceiveMessages();
    }

	private void ContinueCalculations(object obj)
	{
		Start = true;
		Stop = false;
        
		MessagingHelper.SendInitialMessageFromClient(
            NormalizeDuration(PhaseOneDuration, MainWindow.PhaseOneMeasureUnit.Text),
            NormalizeDuration(PhaseTwoDuration, MainWindow.PhaseTwoMeasureUnit.Text),
			QuantityForMechanism,
			NumberOfEpochs - CurrentEpoch,
            Points,
			MainWindow.Mechanism.Text);

		MainWindow.EnableWindowClosing();
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

        MessagingHelper.SendEditMessage(Stop, 
            NormalizeDuration(PhaseOneDuration, MainWindow.PhaseOneMeasureUnit.Text),
            NormalizeDuration(PhaseTwoDuration, MainWindow.PhaseTwoMeasureUnit.Text));
    }

    private static int NormalizeDuration(int duration, string unit)
    {
        return unit switch
        {
            MeasurementUnits.Milliseconds => duration,
            MeasurementUnits.Seconds => duration * 1000,
            _ => duration
        };
    }

    // TODO add : https://learn.microsoft.com/en-us/answers/questions/746124/how-to-disable-and-enable-window-close-button-x-in.html
	private void UpdateCalculations(object obj)
    {
        EditCalculations();
    }

    private void ChooseNumberOfEpochs(object obj)
    {
        var integerUpDown = obj as TextBox;
        PhaseTwoDuration = int.TryParse(integerUpDown?.Text, out var result) ? result : 0;
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
