using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNNWB.Commands;
using CNNWB.Model;

namespace CNNWB.ViewModel
{
    public enum ContextTypes
    {
        Design = 0,
        Training = 1,
        Testing = 2,
        Calculate = 3
    }

    /// <summary>
    /// The main ViewModel class for application.
    /// This class contains the various pages shown
    /// in the workflow and provides navigation
    /// between the pages.
    /// </summary>
    public class PageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private ContextTypes _currentContext;
        private DataProvider _dataProvider;
        private NeuralNetworks _neuralNetwork;
        private ObservableCollection<Button> _commandToolBar;
        private Visibility _commandToolBarVisibility;
        private PageViewModelBase _currentPage;
        private ReadOnlyCollection<PageViewModelBase> _pages;
        private RelayCommand _cancelCommand;
        private RelayCommand _moveToCommand;
        private int _maxDegreeOfParallelism;
        #endregion // Fields

        #region Constructor

        public PageViewModel(DataProvider dataProvider, NeuralNetworks neuralNetwork)
        {
            this.CurrentPage = this.Pages[0];
            this.CurrentPage.CommandToolBar = this.Pages[0].CommandToolBar;

            _maxDegreeOfParallelism = Environment.ProcessorCount;
            this.DataProvider = dataProvider;
            this.NeuralNetwork = neuralNetwork;
        }

        #endregion // Constructor

        #region Events

        /// <summary>
        /// Event definitions.
        /// </summary>
        public event EventHandler RequestClose;
        public event EventHandler PageChange;

        #endregion // Events

        #region Commands

        /// <summary>
        /// Returns the command which, when executed, cancels the order 
        /// and causes the Wizard to be removed from the user interface.
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                    _cancelCommand = new RelayCommand(() => this.Cancel());

                return _cancelCommand;
            }
        }

        public void Cancel()
        {
            this.OnRequestClose();
        }

        public ICommand MoveToCommand(string pageName)
        {
           if (_moveToCommand == null)
                _moveToCommand = new RelayCommand(
                    () => this.MoveToPage(pageName),
                    () => this.CanMoveToPage(pageName));
           
           return _moveToCommand;

        }

        bool CanMoveToPage(string pageName)
        {
            if (this.CurrentPage.IsValid)
            {
                foreach (PageViewModelBase pageView in this.Pages)
                {
                    if (pageView.DisplayName == pageName)
                        return true;
                }
            }

            return false;
        }

        void MoveToPage(string pageName)
        {
            if (CanMoveToPage(pageName))
            {
                foreach (PageViewModelBase pageView in this.Pages)
                {
                    if (pageView.DisplayName == pageName)
                    {
                        this.CurrentPage = pageView;
                        this.CommandToolBar = pageView.CommandToolBar;
                    }
                }
            }
        }

        #endregion // Commands

        #region Properties
        /// <summary>
        /// Returns and sets the current page context used.
        /// </summary>
        public ContextTypes CurrentContext
        {
            get { return _currentContext; }
            set 
            {
                if (value == _currentContext)
                    return;

                _currentContext = value;
                this.OnPropertyChanged("CurrentContext");
            }
        }

        /// <summary>
        /// Returns and sets the dataprovider used.
        /// </summary>
        public DataProvider DataProvider
        {
            get { return _dataProvider; }
            set
            {
                if (value == _dataProvider)
                    return;

                _dataProvider = value;
                foreach (PageViewModelBase model in Pages)
                {
                    model.DataProvider = _dataProvider;
                }
                this.OnPropertyChanged("DataProvider");
            }
        }

        /// <summary>
        /// Returns and sets the neural network used
        /// </summary>
        public NeuralNetworks NeuralNetwork
        {
            get { return _neuralNetwork; }
            set
            {
                if (value == _neuralNetwork)
                    return;

                _neuralNetwork = value;
                foreach (PageViewModelBase model in Pages)
                {
                    model.NeuralNetwork = _neuralNetwork;
                }
                this.OnPropertyChanged("NeuralNetwork");
            }
        }

        /// <summary>
        /// Returns and sets the command toolbar used
        /// </summary>
        public ObservableCollection<Button> CommandToolBar
        {
            get
            {
                if (_commandToolBar == null)
                    _commandToolBar = new ObservableCollection<Button>();

                return _commandToolBar;
            }
            set
            {
                if (value == _commandToolBar)
                    return;

                _commandToolBar = value;
                this.OnPropertyChanged("CommandToolBar");
            }
        }

        public Visibility CommandToolBarVisibility
        {
            get 
            {
                return _commandToolBarVisibility;
            }
            set
            {
                if (value == _commandToolBarVisibility)
                    return;

                _commandToolBarVisibility = value;
                this.OnPropertyChanged("CommandToolBarVisibility");
            }
        }

        /// <summary>
        /// Returns the page ViewModel that the user is currently viewing.
        /// </summary>
        public PageViewModelBase CurrentPage
        {
            get { return _currentPage; }
            set
            {
                if (value == _currentPage)
                    return;

                if (_currentPage != null)
                {
                    _currentPage.IsCurrentPage = false;
                    if (_currentPage.IsValid)
                    {
                        _currentPage = value;
                        this.CommandToolBar = _currentPage.CommandToolBar;
                        this.CommandToolBarVisibility = _currentPage.CommandToolBarVisibility;
                    }
                    else
                        return;
                }
                else
                    _currentPage = value;

                if (_currentPage != null)
                {
                    _currentPage.IsCurrentPage = true;
                    this.CommandToolBar = _currentPage.CommandToolBar;
                    this.CommandToolBarVisibility = _currentPage.CommandToolBarVisibility;
                }
                
                this.OnPropertyChanged("CurrentPage");
                this.OnPageChange();
            }
        }

        /// <summary>
        /// Returns a read-only collection of all page ViewModels.
        /// </summary>
        public ReadOnlyCollection<PageViewModelBase> Pages
        {
            get
            {
                if (_pages == null)
                    this.CreatePages();

                return _pages;
            }
        }


        public int MaximalDegreeOfParallelism
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }

        /// <summary>
        /// Value represents te number of cores used in parallel processing.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get
            {
                return _maxDegreeOfParallelism;
            }
            set
            {
                if (value == _maxDegreeOfParallelism)
                    return;

                
                _maxDegreeOfParallelism = value;

                if (DataProvider != null)
                    DataProvider.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
                if (NeuralNetwork != null)
                    NeuralNetwork.MaxDegreeOfParallelism = _maxDegreeOfParallelism;

                this.OnPropertyChanged("MaxDegreeOfParallelism");
            }
        }
        #endregion


        #region Private Helpers

        void CreatePages()
        {
            var DesignPageVM = new DesignPageViewModel(_dataProvider, _neuralNetwork);
            var TrainingPageVM = new TrainingPageViewModel(_dataProvider, _neuralNetwork);
            var TestingPageVM = new TestingPageViewModel(_dataProvider, _neuralNetwork);
            var CalculatePageVM = new CalculatePageViewModel(_dataProvider, _neuralNetwork);
            
            var pages = new List<PageViewModelBase>();

            pages.Add(DesignPageVM);
            pages[0].CommandToolBar = DesignPageVM.CommandToolBar;
            pages.Add(TrainingPageVM);
            pages[1].CommandToolBar = TrainingPageVM.CommandToolBar;
            pages.Add(TestingPageVM);
            pages[2].CommandToolBar = TestingPageVM.CommandToolBar;
            pages.Add(CalculatePageVM);
            pages[3].CommandToolBar = CalculatePageVM.CommandToolBar;
           
            _pages = new ReadOnlyCollection<PageViewModelBase>(pages);
        }

        int CurrentPageIndex
        {
            get
            {

                if (this.CurrentPage == null)
                {
                    Debug.Fail("Why is the current page null?");
                    return -1;
                }

                return this.Pages.IndexOf(this.CurrentPage);
            }
        }

        void OnRequestClose()
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void OnPageChange()
        {
            CurrentContext = (ContextTypes)CurrentPageIndex;

            EventHandler handler = this.PageChange;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion // Private Helpers

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
