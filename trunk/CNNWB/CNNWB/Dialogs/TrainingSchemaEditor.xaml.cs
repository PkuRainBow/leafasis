using System.Windows;
using System.Collections.ObjectModel;
using CNNWB.Model;
using CNNWB.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Input;
using CNNWB.Common;
using System.IO;

namespace CNNWB.Dialogs
{
    /// <summary>
    /// Interaction logic for TrainingParametersEditor.xaml
    /// </summary>
    public partial class TrainingParametersEditor : Window
    {
        public PageViewModel pageViewModel { get; set; }
        public TrainingRate Data { get; set; }
        public string Path { get; set; }

        private CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
        private CommonSaveFileDialog saveFileDialog = new CommonSaveFileDialog();

        public TrainingParametersEditor()
        {
            Data = new TrainingRate(true);
            InitializeComponent();
        }

        ObservableCollection<TrainingRate> list = new ObservableCollection<TrainingRate>();
 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Data != null)
                list.Add(Data);

            dataGrid1.ItemsSource = list;
        }

        private void buttonTrain_Click(object sender, RoutedEventArgs e)
        {
            bool first = true;
            foreach (TrainingRate rate in list)
            {
                pageViewModel.NeuralNetwork.AddGlobalTrainingRate(rate, first);
                first = false;
            }
            DialogResult = true;
            this.Close();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
           
            saveFileDialog.InitialDirectory = Path;
            saveFileDialog.Filters.Add(new CommonFileDialogFilter("Xml Training Schema", "schema-xml"));
            saveFileDialog.DefaultExtension = "schema-xml";
            saveFileDialog.AlwaysAppendDefaultExtension = true;
            saveFileDialog.AllowPropertyEditing = false;
            saveFileDialog.ShowPlacesList = true;
            saveFileDialog.CreatePrompt = false;
            saveFileDialog.OverwritePrompt = true;


            if (saveFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string fileName = saveFileDialog.FileAsShellObject.ParsingName;

                Mouse.OverrideCursor = Cursors.Wait;
                if (fileName.Contains("schema-xml"))
                {
                    using (NeuralNetworkDataSet.TrainingRatesDataTable table = new NeuralNetworkDataSet.TrainingRatesDataTable())
                    {
                        table.BeginLoadData();
                        foreach (TrainingRate rate in list)
                        {
                            table.AddTrainingRatesRow(rate.Rate, rate.Epochs, rate.MinimumRate, rate.DecayFactor, rate.DecayAfterEpochs, rate.WeightSaveTreshold, rate.Distorted, rate.SameDistortionsForNEpochs, rate.SeverityFactor, rate.MaxScaling, rate.MaxRotation, rate.ElasticSigma, rate.ElasticScaling);
                        }
                        table.EndLoadData();

                        table.WriteXml(fileName, System.Data.XmlWriteMode.WriteSchema);
                    }
                    Mouse.OverrideCursor = null;
                    InformationDialog.Show(this, "Training Schema is saved.", "", "Information");
                }

                Mouse.OverrideCursor = null;
            }
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog.InitialDirectory = Path;
            openFileDialog.Filters.Add(new CommonFileDialogFilter("Xml Training Schema", "schema-xml"));
            openFileDialog.Title = "Load Training Schema";
            openFileDialog.DefaultExtension = "schema-xml";
            openFileDialog.AllowNonFileSystemItems = true;
            openFileDialog.EnsureFileExists = true;
            openFileDialog.EnsurePathExists = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string fileName = openFileDialog.FileAsShellObject.ParsingName;

                if (fileName.Contains(".schema-xml"))
                {

                    using (NeuralNetworkDataSet.TrainingRatesDataTable table = new NeuralNetworkDataSet.TrainingRatesDataTable())
                    {
                        table.ReadXml(fileName);

                        list.Clear();
                        foreach (NeuralNetworkDataSet.TrainingRatesRow row in table)
                        {
                            list.Add(new TrainingRate(row.Rate, row.Epochs, row.MinimumRate, row.DecayFactor, row.DecayAfterEpochs, row.WeightSaveTreshold, row.Distorted, row.SameDistortionsForEpochs, row.SeverityFactor, row.MaxScaling, row.MaxRotation, row.ElasticSigma, row.ElasticScaling));
                        }
                    }
                    Mouse.OverrideCursor = null;
                    InformationDialog.Show(this, "Training Schema is loaded.", "Information");
                }
            }
            Mouse.OverrideCursor = null;
        }
    }
}
