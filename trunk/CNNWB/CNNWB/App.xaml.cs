using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using System.Windows.Input;

namespace CNNWB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable 
    {
        MainViewWindow mainWindow; 

        static App()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        ~App()
		{
			// In case the client forgets to call
			// Dispose , destructor will be invoked for
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Free managed objects.
                mainWindow.Closing -= mainWindow_Closing;
                mainWindow.Dispose();
			}
			// Free unmanaged objects
		}

		public void Dispose()
		{
			Dispose(true);
			// Ensure that the destructor is not called
			GC.SuppressFinalize(this);
		}

        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyHandleMouseButton), true);
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);

            base.OnStartup(e);

            mainWindow = new MainViewWindow();
            mainWindow.Closing += mainWindow_Closing;
            mainWindow.Show();
        }

        private void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                using (TaskDialog quitApplicationDialog = new TaskDialog())
                {
                    quitApplicationDialog.Caption = "Exit application";
                    quitApplicationDialog.Icon = TaskDialogStandardIcon.None;
                    quitApplicationDialog.StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No;
                    quitApplicationDialog.Cancelable = true;
                    quitApplicationDialog.Text = "Do you really want to quit the application ?";
                    quitApplicationDialog.OwnerWindowHandle = new WindowInteropHelper(mainWindow).Handle;
                    quitApplicationDialog.StartupLocation = TaskDialogStartupLocation.CenterOwner;
                    if (quitApplicationDialog.Show() == TaskDialogResult.Yes)
                        e.Cancel = false;
                    else
                        e.Cancel = true;
                }
            }
            catch (Exception)
            {
                if (MessageBox.Show("Do you really want to quit the application ?", "Exit application", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    e.Cancel = false;
                else
                    e.Cancel = true;
            }
        }

        private static void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e)
        {
            var textbox = (sender as TextBox);
            if (textbox != null && !textbox.IsKeyboardFocusWithin)
            {
                if (e.OriginalSource.GetType().Name == "TextBoxView")
                {
                    e.Handled = true;
                    textbox.Focus();
                }
            }
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
            {
                if (!textBox.IsReadOnly)
                    textBox.SelectAll();
            }
        } 

    }
}
