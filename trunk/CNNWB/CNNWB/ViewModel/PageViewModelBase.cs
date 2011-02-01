using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CNNWB.Model;

namespace CNNWB.ViewModel
{
    /// <summary>
    /// Abstract base class for all pages shown in the application MainWindow.
    /// </summary>
    public abstract class PageViewModelBase: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields

        private DataProvider _dataProvider;
        private NeuralNetworks _neuralNetwork;
        private ObservableCollection<Button> _commandToolBar;
        private Visibility _commandToolBarVisibility;
        private bool _isCurrentPage;
        private bool _isValid = true;
        
        #endregion // Fields

        #region Events

        /// <summary>
        /// Event definitions.
        /// </summary>
        public event EventHandler NeuralNetworkChanged;
        #endregion // Events

        #region Constructor

        protected PageViewModelBase(DataProvider dataProvider, NeuralNetworks neuralNetwork)
        {
            _dataProvider = dataProvider;
            _neuralNetwork = neuralNetwork;
            _commandToolBarVisibility = Visibility.Hidden;
            _commandToolBar = new ObservableCollection<Button>();
            _commandToolBar.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_commandToolBar_CollectionChanged);
            
        }

        #endregion // Constructor

        #region Event Handlers
        void OnNeuralNetworkChanged()
        {
            EventHandler handler = this.NeuralNetworkChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        #endregion // Event Handlers 

        #region Properties

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
                this.OnPropertyChanged("NeuralNetwork");
                this.OnNeuralNetworkChanged();
            }
        }

        public Visibility CommandToolBarVisibility
        {
            get { return _commandToolBarVisibility; }
 
            set
            {
                if (_commandToolBarVisibility == value)
                    return;

                _commandToolBarVisibility = value;
                this.OnPropertyChanged("CommandToolBarVisibility");
            }
        }

        /// <summary>
        /// Returns and sets the command toolbar used
        /// </summary>
        public ObservableCollection<Button> CommandToolBar
        {
            get
            {
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

        public abstract string DisplayName { get; }

        public bool IsCurrentPage
        {
            get { return _isCurrentPage; }
            set 
            {
                if (value == _isCurrentPage)
                    return;

                _isCurrentPage = value;
                this.OnPropertyChanged("IsCurrentPage");
            }
        }


        /// <summary>
        /// Returns false if the user has not completed this page properly
        /// and the application should not allow the user to progress to an 
        /// other page in the workflow.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                if (value == _isValid)
                    return;

                _isValid = value;
                this.OnPropertyChanged("IsValid");
            }
        }
        #endregion // Properties

        private void _commandToolBar_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CommandToolBar.Count > 0)
                CommandToolBarVisibility = Visibility.Visible;
            else
                CommandToolBarVisibility = Visibility.Collapsed;
        }

        #region INotifyPropertyChanged Members

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // If you raise PropertyChanged and do not specify a property name,
            // all properties on the object are considered to be changed by the binding system.
            if (String.IsNullOrEmpty(propertyName))
                return;

            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new ArgumentException(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #endregion // INotifyPropertyChanged Members
    }
}
