using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CNNWB.Common;


namespace CNNWB
{
    /// <summary>
    /// Interaction logic for DownloadMNIST.xaml
    /// </summary>
    public partial class DownloadMNIST : Window
    {
        public string Path;
        public bool AllDownloaded = false;
        
        private delegate void ShowProgressDelegate(System.Object sender, DownloadProgressChangedEventArgs e);
        private delegate void DownloadFinishedDelegate(System.Object sender, DownloadDataCompletedEventArgs e);

        private ShowProgressDelegate showProgress;
        private DownloadFinishedDelegate downloadFinished;

        private List<String> Files;
        private string baseUrl;
        private bool TestingLabelsFinished = false;
        private bool TrainingLabelsFinished = false;
        private bool TestingImagesFinished = false;
        private bool TrainingImagesFinished = false;

        public DownloadMNIST()
        {
            baseUrl = @"http://yann.lecun.com/exdb/mnist/";
            Files = new List<string>(4);
            Files.Add("t10k-labels-idx1-ubyte.gz");
            Files.Add("train-labels-idx1-ubyte.gz");
            Files.Add("t10k-images-idx3-ubyte.gz");
            Files.Add("train-images-idx3-ubyte.gz");

            System.Net.ServicePointManager.DefaultConnectionLimit = 10;
            showProgress = ShowProgress;
            downloadFinished = DownloadFinished;

            InitializeComponent();
            buttonOK.IsEnabled = AllDownloaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int index=0; index < 4; index++)
            {
                Task.Factory.StartNew(() => Download(baseUrl, index));
                Thread.Sleep(100);
            }
        }

        public void Download(string baseAddress, int fileIndex)
        {
            using (WebClient client = new WebClient())
            {
                client.UseDefaultCredentials = true;

                client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(client_DownloadDataCompleted);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.BaseAddress = baseAddress;

                try
                {
                    client.DownloadDataAsync(new Uri(Files[fileIndex],UriKind.Relative), fileIndex);
                }
                catch (Exception exception)
                {
                    InformationDialog.Show(this, exception.ToString(), exception.Message);
                    this.Close();
                }
            }
        }

        private void client_DownloadProgressChanged(System.Object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(showProgress, System.Windows.Threading.DispatcherPriority.Render, new object[] { sender, e });

        }
        private void client_DownloadDataCompleted(System.Object sender, DownloadDataCompletedEventArgs e)
        {
            Dispatcher.Invoke(downloadFinished, System.Windows.Threading.DispatcherPriority.Render, new object[] { sender, e });
        }

        private void ShowProgress(System.Object sender, DownloadProgressChangedEventArgs e)
        {
            double kilobytesReceived = Math.Truncate(double.Parse(e.BytesReceived.ToString()) / 1024D);
            double totalKiloBytesToReceive = Math.Truncate(double.Parse(e.TotalBytesToReceive.ToString()) / 1024D);
            string kbyte = " KByte)";
            
            switch ((int)e.UserState)
            {
                case 0:
                    textBlockTestingLabels.Text = "Downloading Testing Labels (" + kilobytesReceived.ToString() + "/" + totalKiloBytesToReceive.ToString() + kbyte;
                    progressBarTestingLabels.Value = e.ProgressPercentage;
                    break;

                case 1:
                    textBlockTrainingLabels.Text = "Downloading Training Labels (" + kilobytesReceived.ToString() + "/" + totalKiloBytesToReceive.ToString() + kbyte;
                    progressBarTrainingLabels.Value = e.ProgressPercentage;
                    break;

                case 2:
                    textBlockTestingImages.Text = "Downloading Testing Images (" + kilobytesReceived.ToString() + "/" + totalKiloBytesToReceive.ToString() + kbyte;
                    progressBarTestingImages.Value = e.ProgressPercentage;
                    break;

                case 3:
                    textBlockTrainingImages.Text = "Downloading Training Images (" + kilobytesReceived.ToString() + "/" + totalKiloBytesToReceive.ToString() + kbyte;
                    progressBarTrainingImages.Value = e.ProgressPercentage;
                    break;
            }
        }

        private void DownloadFinished(System.Object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {

                string fileName = Files[(int)e.UserState].Remove(Files[(int)e.UserState].Length - 3, 3);
                using (MemoryStream memoryStream = new MemoryStream(e.Result, false))
                {
                    using (System.IO.Compression.GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress, false))
                    {
                        using (System.IO.FileStream fileStream = new FileStream(Path + @"\" + fileName, FileMode.Create, FileAccess.ReadWrite))
                        {
                            zipStream.CopyTo(fileStream);
                            fileStream.Flush(true);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                InformationDialog.Show(this, exception.ToString(), exception.Message);
                this.Close();
            }
            finally
            {
                (sender as WebClient).DownloadProgressChanged -= new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                (sender as WebClient).DownloadDataCompleted -= new DownloadDataCompletedEventHandler(client_DownloadDataCompleted);
            }

            switch ((int)e.UserState)
            {
                case 0:
                    TestingLabelsFinished = true;
                    break;

                case 1:
                    TrainingLabelsFinished = true;
                    break;

                case 2:
                    TestingImagesFinished = true;
                    break;

                case 3:
                    TrainingImagesFinished = true;
                    break;
            }

            AllDownloaded = (TestingLabelsFinished && TrainingLabelsFinished && TestingImagesFinished && TrainingImagesFinished);
            
            buttonOK.IsEnabled = AllDownloaded;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
