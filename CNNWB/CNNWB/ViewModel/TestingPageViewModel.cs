using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CNNWB.Commands;
using CNNWB.Common;
using CNNWB.Model;

namespace CNNWB.ViewModel
{
    public enum TestingStates
    {
        Idle = 0,
        PreparingData = 1,
        Testing = 2,
    }

    public class TestingPageViewModel : PageViewModelBase
    {
        private TestingStates _currentState = TestingStates.Idle;
        private String _progressText;
        private ImageSource _progressImage;
        private String _progressImageNumber;
        private ConfusionMatrix _confusionMatrix;
        private string _confusionDocument;
        private bool _useDistortions = false;
        private bool _useTrainingSet = false;
        private RelayCommand _startCommand;
        private RelayCommand _stopCommand;
        private int _selectedTabIndex = -1;
        private double _severityFactor = 1D;
        private double _maxScaling = 15D;
        private double _maxRotation = 15D;
        private double _elasticSigma = 8.0D;
        private double _elasticScaling= 0.5D;

        #region Events
        /// <summary>
        /// Event definitions.
        /// </summary>
        public event EventHandler Start;
        public event EventHandler Stop;
        #endregion // Events

        public TestingPageViewModel()
            : base(null,  null)
        {
            UseTrainingSet = false;
            AddCommandButtons();
        }

        public TestingPageViewModel(DataProvider dataProvider, NeuralNetworks neuralNetwork)
            : base(dataProvider, neuralNetwork)
        {
            UseTrainingSet = false;
            _confusionMatrix = new ConfusionMatrix("ConfusionMatrix", dataProvider);
            AddCommandButtons();
        }

        private void AddCommandButtons()
        {
            Button startButton = new Button();
            startButton.Name = "ButtonStart";
            startButton.ToolTip = "Start testing";
            startButton.Content = new BitmapToImage(CNNWB.Properties.Resources.PlayHS);
            startButton.Click += new RoutedEventHandler(StartButtonClick);
            Button stopButton = new Button();
            stopButton.Name = "ButtonStop";
            stopButton.ToolTip = "Stop testing";
            stopButton.Content = new BitmapToImage(CNNWB.Properties.Resources.StopHS);
            stopButton.Click += new RoutedEventHandler(StopButtonClick);
            CommandToolBar.Add(startButton);
            CommandToolBar.Add(stopButton);
        }

        public TestingStates CurrentState
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

        public ConfusionMatrix ConfusionMatrix
        {
            get { return _confusionMatrix; }
            set
            {
                if (value == _confusionMatrix)
                    return;

                _confusionMatrix = value;
                this.OnPropertyChanged("ConfusionMatrix");
            }
        }

        public string ConfusionDocument
        {
            get { return _confusionDocument; }
            set
            {
                if (value == _confusionDocument)
                    return;

                _confusionDocument = value;
                this.OnPropertyChanged("ConfusionDocument");
            }
        }

        public bool UseDistortions
        {
            get { return _useDistortions; }
            set
            {
                if (value == _useDistortions)
                    return;

                _useDistortions = value;
                this.OnPropertyChanged("UseDistortions");
            }
        }

        public bool UseTrainingSet
        {
            get { return _useTrainingSet; }
            set
            {
                if (value == _useTrainingSet)
                    return;

                _useTrainingSet = value;
                this.OnPropertyChanged("UseTrainingSet");
            }
        }

        public double SeverityFactor
        {
            get { return _severityFactor; }
            set
            {
                if (value == _severityFactor)
                    return;

                _severityFactor = value;
                this.OnPropertyChanged("SeverityFactor");
            }

        }

        public double MaxScaling
        {
            get { return _maxScaling; }
            set
            {
                if (value == _maxScaling)
                    return;

                _maxScaling = value;
                this.OnPropertyChanged("MaxScaling");
            }
        }

        public double MaxRotation
        {
            get { return _maxRotation; }
            set
            {
                if (value == _maxRotation)
                    return;

                _maxRotation = value;
                this.OnPropertyChanged("MaxRotation");
            }
        }

        public double ElasticSigma
        {
            get { return _elasticSigma; }
            set
            {
                if (value == _elasticSigma)
                    return;

                _elasticSigma = value;
                this.OnPropertyChanged("ElasticSigma");
            }
        }

        public double ElasticScaling
        {
            get { return _elasticScaling; }
            set
            {
                if (value == _elasticScaling)
                    return;

                _elasticScaling = value;
                this.OnPropertyChanged("ElasticScaling");
            }
        }

        public override string DisplayName
        {
            get { return "Testing"; }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if (value == _selectedTabIndex)
                    return;

                _selectedTabIndex = value;
                this.OnPropertyChanged("SelectedTabIndex");
            }
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

        public void StartCmd()
        {
            this.OnStart();
        }

        public void StopCmd()
        {
            this.OnStop();
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

        void StartButtonClick(object sender, RoutedEventArgs e)
        {
            StartCmd();
        }

        void StopButtonClick(object sender, RoutedEventArgs e)
        {
            StopCmd();
        }
    }
}
