using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CNNWB.Commands;
using CNNWB.Common;
using CNNWB.Model;
using Swordfish.WPF.Charts;

namespace CNNWB.ViewModel
{
    public enum TrainingStates
    {
        Idle = 0,
        NewEpoch = 1,
        PreparingData = 2,
        CalculatingHessian = 3,
        Training = 4,
        CalculatingTestError = 5,
        SavingWaits = 6
    }

    public class TrainingPageViewModel : PageViewModelBase
    {
        private TrainingStates _currentState = TrainingStates.Idle;
        private String _progressText;
        private ImageSource _progressImage;
        private String _progressImageNumber; 
        private ObservableCollection<TrainingResult> _learningResultCollection;
        private RelayCommand _startCommand;
        private RelayCommand _stopCommand;
        private RelayCommand _editorCommand;
        private XYLineChart _xyLineChart;

        #region Events
        /// <summary>
        /// Event definitions.
        /// </summary>
        public event EventHandler Start;
        public event EventHandler Stop;
        public event EventHandler Editor;
        #endregion // Events

        public TrainingPageViewModel()
            : base(null,  null)
        {
            TrainingResultCollection = new ObservableCollection<TrainingResult>();
            AddCommandButtons();
        }

        public TrainingPageViewModel(DataProvider dataProvider, NeuralNetworks neuralNetwork)
            : base(dataProvider, neuralNetwork)
        {
            TrainingResultCollection = new ObservableCollection<TrainingResult>();

            setupChart();
            AddCommandButtons();
        }
        private void setupChart()
        {
            XYLineChart chart = new XYLineChart();
            chart.Height = 126; chart.Width = 486;
            
            chart.RenderTransformOrigin = new Point(0.5,0.5);
            // Add test Lines to demonstrate the control

            chart.Primitives.Clear();

            // Create 3 normal lines
            ChartPrimitive[] lines = new ChartPrimitive[3];

      
            for (int lineNo = 0; lineNo < 3; ++lineNo)
            {
                ChartPrimitive line = new ChartPrimitive();

                // Label the lines
                //line.Label = "Test Line " + (lineNo + 1);
                line.ShowInLegend = true;
                //line.HitTest = true;

                line.LineThickness = 1.5;
                // Draw 3 sine curves
                //for (double x = 0; x < limit + increment * .5; x += increment)
                //{
                //    line.AddPoint(x, 1);
                //}
                //line.AddPoint(0, 1);
                // Add the lines to the chart
                chart.Primitives.Add(line);
                lines[lineNo] = line;
            }
            // Set the line colors to Red, Green, and Blue
            lines[0].Label = "TrainingRate";
            lines[1].Label = "AvgTrainMSE";
            lines[2].Label = "AvgTestMSE";
            lines[0].AddPoint(0, 2);
            lines[0].AddPoint(33, 2);
            lines[1].AddPoint(0, 1.5);
            lines[1].AddPoint(33, 1.5);
            lines[2].AddPoint(0, 1);
            lines[2].AddPoint(33, 1);
            
            lines[0].Color = Colors.Red;
            lines[1].Color = Colors.Green;
            lines[2].Color = Colors.Blue;

            chart.Title = "TrainingParameters";
            chart.XAxisLabel = "TrainingEpoch";
            chart.YAxisLabel = "Value";

            chart.RedrawPlotLines();
            lines[0].Points.Clear();
            lines[1].Points.Clear();
            lines[2].Points.Clear();
            //lines[0].AddPoint(0, 0);
            //lines[1].AddPoint(0, 0);
            //lines[2].AddPoint(0, 0);
            //_xyLineChart = chart;
            Chart = chart;
        }
        private void AddCommandButtons()
        {
            Button startButton = new Button();
            startButton.Name = "ButtonStart";
            startButton.ToolTip = "Start training";
            startButton.Content = new BitmapToImage(CNNWB.Properties.Resources.PlayHS);
            startButton.Click += new RoutedEventHandler(StartButtonClick);
            Button stopButton = new Button();
            stopButton.Name = "ButtonStop";
            stopButton.ToolTip = "Stop training";
            stopButton.Content = new BitmapToImage(CNNWB.Properties.Resources.StopHS);
            stopButton.Click += new RoutedEventHandler(StopButtonClick);
            Button editorButton = new Button();
            editorButton.Name = "ButtonEditor";
            editorButton.ToolTip = "Training Schema Editor";
            editorButton.Content = new BitmapToImage(CNNWB.Properties.Resources.Properties);
            editorButton.Click += new RoutedEventHandler(EditorButtonClick);
            CommandToolBar.Add(startButton);
            CommandToolBar.Add(stopButton);
            CommandToolBar.Add(editorButton);
        }

        public XYLineChart Chart
        {
            get { return _xyLineChart; }
            set
            {
                if (value == _xyLineChart)
                    return;
                _xyLineChart = value;
                this.OnPropertyChanged("Chart");
            }
        }
        public void DrawChart(TrainingResult trainResult)
        {
            if (_xyLineChart == null)
                return;
            //TrainingRate
            ChartPrimitive line0 = _xyLineChart.Primitives[0];
            line0.AddPoint(trainResult.Epoch,trainResult.TrainingRate*1000);
            ChartPrimitive line1 = _xyLineChart.Primitives[1];
            line1.AddPoint(trainResult.Epoch, trainResult.AvgTrainMSE);
            ChartPrimitive line2 = _xyLineChart.Primitives[2];
            line2.AddPoint(trainResult.Epoch, trainResult.AvgTestMSE);
            _xyLineChart.RedrawPlotLines();

        }
        public TrainingStates CurrentState
        {
            get { return _currentState; }
            set
            {
                if (value == _currentState)
                    return;

                _currentState = value;
                this.OnPropertyChanged("CurrentState");
            }
        }

        public string ProgressText
        {
            get
            {
                return _progressText;
            }
            set
            {
                if (value == _progressText)
                    return;

                _progressText = value;
                this.OnPropertyChanged("ProgressText");
            }
        }

        public ImageSource ProgressImage
        {
            get { return _progressImage; }
            set
            {
                if (value == _progressImage)
                    return;

                _progressImage = value;
                this.OnPropertyChanged("ProgressImage");
            }
        }

        public string ProgressImageNumber
        {
            get
            {
                return _progressImageNumber;
            }
            set
            {
                if (value == _progressImageNumber)
                    return;

                _progressImageNumber = value;
                this.OnPropertyChanged("ProgressImageNumber");
            }
        }

        public ObservableCollection<TrainingResult> TrainingResultCollection 
        {
            get { return _learningResultCollection; }
            private set
            {
                if (value == _learningResultCollection)
                    return;

                _learningResultCollection = value;
                this.OnPropertyChanged("TrainingResultCollection");
            }
        }

        public override string DisplayName
        {
            get { return "Training"; }
        }

        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                    _startCommand = new RelayCommand(() => this.StartCmd());

                return _startCommand;
            }
        }

        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                    _stopCommand = new RelayCommand(() => this.StopCmd());

                return _stopCommand;
            }
        }

        public ICommand EditorCommand
        {
            get
            {
                if (_editorCommand == null)
                    _editorCommand = new RelayCommand(() => this.EditorCmd());

                return _editorCommand;
            }
        }

        public void StartCmd()
        {
            this.OnStart();
        }

        public void StopCmd()
        {
            this.OnStop();
        }

        public void EditorCmd()
        {
            this.OnEditor();
        }

        void OnStart()
        {
            EventHandler handler = this.Start;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void OnStop()
        {
            EventHandler handler = this.Stop;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void OnEditor()
        {
            EventHandler handler = this.Editor;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void StartButtonClick(object sender, RoutedEventArgs e)
        {
            StartCmd();
        }

        void StopButtonClick(object sender, RoutedEventArgs e)
        {
            StopCmd();
        }

        void EditorButtonClick(object sender, RoutedEventArgs e)
        {
            EditorCmd();
        }
    }
}
