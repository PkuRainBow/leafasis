using System.Windows;
using System.Windows.Controls;
using CNNWB.ViewModel;

namespace CNNWB.View
{
    /// <summary>
    /// Interaction logic for CalculatePageView.xaml
    /// </summary>
    public partial class CalculatePageView : UserControl
    {
        public CalculatePageView()
        {
            InitializeComponent();
            checkBoxDistortImage_CheckedChanged(this, new RoutedEventArgs());
        }

        private void radioButtonTestingTrainingSet_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
                (this.DataContext as CalculatePageViewModel).UseTrainingSet = true;
        }

        private void radioButtonTestingTestingSet_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
                (this.DataContext as CalculatePageViewModel).UseTrainingSet = false;
        }

        private void checkBoxDistortImage_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = false;

            if (this.DataContext != null)
                enabled = (this.DataContext as CalculatePageViewModel).IsValid;

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

        private void textBoxPatternIndex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.DataContext != null)
            {
                int index = -1;
                if (int.TryParse(textBoxPatternIndex.Text, out index))
                {
                    if (index >= 0)
                    {
                        if (((this.DataContext as CalculatePageViewModel).UseTrainingSet) && index >= 60000)
                        {
                            textBoxPatternIndex.Text = string.Empty;
                            e.Handled = true;
                            return;
                        }
                        if (!((this.DataContext as CalculatePageViewModel).UseTrainingSet) && index >= 10000)
                        {
                            textBoxPatternIndex.Text = string.Empty;
                            e.Handled = true;
                            return;
                        }
                        (this.DataContext as CalculatePageViewModel).PatternIndex = index;
                        e.Handled = true;
                    }
                    else
                    {
                        textBoxPatternIndex.Text = string.Empty;
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    textBoxPatternIndex.Text = string.Empty;
                    e.Handled = true;
                }
            }
        }
    }
}
