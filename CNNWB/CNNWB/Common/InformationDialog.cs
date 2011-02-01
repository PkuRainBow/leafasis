using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Interop;

namespace CNNWB.Common
{
    public class InformationDialog
    {
        public static void Show(Window owner = null, string text = "", string instructionText = "", string caption = "Information")
        {
            IntPtr windowHandle = IntPtr.Zero;

            if (owner == null)
                windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            else
                windowHandle = new WindowInteropHelper(owner).Handle;

            try
            {
                using (TaskDialog taskDialog = new TaskDialog())
                {
                    taskDialog.OwnerWindowHandle = windowHandle;
                    taskDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    taskDialog.StartupLocation = TaskDialogStartupLocation.CenterOwner;
                    taskDialog.Icon = TaskDialogStandardIcon.Information;
                    taskDialog.Text = text;
                    taskDialog.InstructionText = instructionText;
                    taskDialog.Caption = caption;
                    taskDialog.Show();
                }
            }
            catch (Exception)
            {
                if (owner != null)
                    MessageBox.Show(owner, text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
