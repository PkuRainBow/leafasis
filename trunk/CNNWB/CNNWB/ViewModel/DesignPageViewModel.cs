using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using CNNWB.Model;

namespace CNNWB.ViewModel
{
    class DesignPageViewModel : PageViewModelBase
    {
        private string _description = String.Empty;
        private ObservableCollection<TabItem> _tabItems = null;
        private int _selectedTabIndex = -1;

        public DesignPageViewModel()
            : base(null,  null)
        {
            base.OnPropertyChanged("NeuralNetwork");
        }

        public DesignPageViewModel(DataProvider dataProvider, NeuralNetworks neuralNetwork)
            : base(dataProvider, neuralNetwork)
        {
            NeuralNetworkChanged += this.OnNeuralNetworkChanged;
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (_description == value)
                    return;

                _description = value;
                OnPropertyChanged("Description");
            }
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

        public override string DisplayName
        {
            get { return "Design"; }
        }

        private void OnNeuralNetworkChanged(object sender, EventArgs e)
        {
            Description = NeuralNetwork.Description;
        }
    }
}
