using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CNNWB.Commands;
using CNNWB.Common;
using CNNWB.Model;

namespace CNNWB.ViewModel
{
    class CalculatePageViewModel : PageViewModelBase
    {
        private int _patternIndex = 0;
        private string _result = String.Empty;
        private bool _useDistortions = false;
        private bool _useTrainingSet = false;
        private ObservableCollection<TabItem> _tabItems = null;
        private double _severityFactor = 1D;
        private double _maxScaling = 15D;
        private double _maxRotation = 15D;
        private double _elasticSigma = 8.0D;
        private double _elasticScaling = 0.5D;
        private RelayCommand _calculateCommand;
        private int _selectedTabIndex = -1;

        #region Events
        /// <summary>
        /// Event definitions.
        /// </summary>
        public event EventHandler Calculate;
        #endregion // Events

        public CalculatePageViewModel()
            : base(null,  null)
        {
            AddCommandButtons();
        }

        public CalculatePageViewModel(DataProvider dataProvider, NeuralNetworks neuralNetwork)
            : base(dataProvider, neuralNetwork)
        {
            AddCommandButtons();
        }

        private void AddCommandButtons()
        {
            Button calculateButton = new Button();
            calculateButton.Name = "ButtonCalculate";
            calculateButton.ToolTip = "Calculate network";
            calculateButton.Content = new BitmapToImage(CNNWB.Properties.Resources.CalculatorHS);
            calculateButton.Click += new RoutedEventHandler(CalculateButtonClick);
            CommandToolBar.Add(calculateButton);
        }

        public ObservableCollection<TabItem> TabItems
        {
            get { return _tabItems; }
            set
            {
                if (value == _tabItems)
                    return;

                _tabItems = value;
                this.OnPropertyChanged("TabItems");
            }
        }

        public string Result
        {
            get { return _result; }
            set
            {
                if (value == _result)
                    return;

                _result = value;
                this.OnPropertyChanged("Result");
            }
        }

        public int PatternIndex
        {
            get { return _patternIndex; }
            set
            {
                if (value == _patternIndex )
                    return;

                _patternIndex = value;
                this.OnPropertyChanged ("PatternIndex");
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
            get { return "Calculate"; }
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

        public ICommand CalculateCommand
        {
            get
            {
                if (_calculateCommand == null)
                    _calculateCommand = new RelayCommand(() => this.CalculateCmd());

                return _calculateCommand;
            }
        }

        public void CalculateCmd()
        {
            this.OnCalculate();
        }

        void OnCalculate()
        {
            EventHandler handler = this.Calculate;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void CalculateButtonClick(object sender, RoutedEventArgs e)
        {
            CalculateCmd();
        }
    }
}
