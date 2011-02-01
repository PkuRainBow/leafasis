using System.Windows;
using System.Windows.Controls;
using CNNWB.ViewModel;

namespace CNNWB.View
{
    /// <summary>
    /// Interaction logic for TestingPageView.xaml
    /// </summary>
    public partial class TestingPageView : UserControl
    {
        public TestingPageView()
        {
            InitializeComponent();
            checkBoxTestingDistortImage_CheckedChanged(this, new RoutedEventArgs());
        }

        private void radioButtonTestingTrainingSet_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
                (this.DataContext as TestingPageViewModel).UseTrainingSet = true;
        }

        private void radioButtonTestingTestingSet_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
                (this.DataContext as TestingPageViewModel).UseTrainingSet = false;
        }

        private void checkBoxTestingDistortImage_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = false;

            if (this.DataContext != null)
                enabled = (this.DataContext as TestingPageViewModel).IsValid;

            if (enabled)
            {
                groupBoxDistortionParameters.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
                //textBoxSeverityFactor.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
                //textBoxMaximumScaling.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
                //textBoxMaximumRotation.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
                //textBoxElasticSigma.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
                //textBoxElasticScaling.IsEnabled = checkBoxTestingDistortImage.IsChecked ?? false;
            }
            else
            {
                groupBoxDistortionParameters.IsEnabled = enabled;
                //textBoxSeverityFactor.IsEnabled = enabled;
                //textBoxMaximumScaling.IsEnabled = enabled;
                //textBoxMaximumRotation.IsEnabled = enabled;
                //textBoxElasticSigma.IsEnabled = enabled;
                //textBoxElasticScaling.IsEnabled = enabled;
            }
        }

       
    }
}
