using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CNNWB.Model;
using CNNWB.Dialogs;
using CNNWB.ViewModel;

namespace CNNWB
{
    /// <summary>
    /// Interaction logic for TrainingParameters.xaml
    /// </summary>
    public partial class TrainingParameters : Window
    {
        public PageViewModel pageViewModel { get; set; }
        public TrainingRate Data { get; set; }

        public TrainingParameters()
        {
            InitializeComponent();

            Data = new TrainingRate();
            DataContext = Data;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if (IsValid(this))
            {
                pageViewModel.NeuralNetwork.AddGlobalTrainingRate(Data, true);
                DialogResult = true;
                this.Close();
            }
        }

        bool IsValid(DependencyObject node)
        {
            // Check if dependency object was passed
            if (node != null)
            {
                // Check if dependency object is valid.
                // NOTE: Validation.GetHasError works for controls that have validation rules attached 
                bool isValid = !Validation.GetHasError(node);
                if (!isValid)
                {
                    // If the dependency object is invalid, and it can receive the focus,
                    // set the focus
                    if (node is IInputElement) Keyboard.Focus((IInputElement)node);
                    return false;
                }
            }

            // If this dependency object is valid, check all child dependency objects
            foreach (object subnode in LogicalTreeHelper.GetChildren(node))
            {
                if (subnode is DependencyObject)
                {
                    // If a child dependency object is invalid, return false immediately,
                    // otherwise keep checking
                    if (IsValid((DependencyObject)subnode) == false) return false;
                }

            }

            // All dependency objects are valid
            return true;
        }

        private void checkBoxUseDistortions_CheckedChanged(object sender, RoutedEventArgs e)
        {
            textBoxSameDistortionsForEpochs.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
            textBoxSeverityFactor.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
            textBoxMaximumScaling.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
            textBoxMaximumRotation.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
            textBoxElasticSigma.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
            textBoxElasticScaling.IsEnabled = checkBoxUseDistortions.IsChecked ?? false;
        }
    }
}
