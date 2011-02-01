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
            AddCommandButtons();
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
