using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CNNWB.Common;
using CNNWB.Dialogs;
using CNNWB.Model;
using CNNWB.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Net;
using Microsoft.WindowsAPICodePack.Taskbar;
//using Word = Microsoft.Office.Interop.Word;

namespace CNNWB
{
	public delegate void Progress();
	public delegate void AddErrorItem(int a, int b, int c);
	public delegate void Calculation();
	/// <summary>
	/// Interaction logic for MainViewWindow.xaml
	/// </summary>
	public partial class MainViewWindow : Window, IDisposable 
	{
		public static RoutedCommand ApplicationExitCmd = new RoutedCommand();
		public static RoutedCommand PageSetupCmd = new RoutedCommand();
		public static RoutedCommand AboutCmd = new RoutedCommand();

		private readonly PageViewModel pageViewModel;

		public static string ApplicationStorageDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\CNNWB";
        public static string MNISTDirectory = ApplicationStorageDirectory + @"\MNIST Dataset";
        public static string TrainingSchemasDirectory = ApplicationStorageDirectory + @"\Training Schemas";
		private TaskbarManager taskbarManager;

		private StringBuilder LocalTime = new StringBuilder(8);
		
		private bool trainingStopped = true;
		private bool testingStopped = true;

		private System.Timers.Timer workerTimer;
		private Progress workerProgress;
		private AddErrorItem addErrorItem;
		private Thread workerThread;
		
		private DateTime start;
		private DateTime startSpan;
		private double avgMSE;
		private double avgTestMSE;
		private double prevAvgMSE;
		private int curEpoch;
		private int curIndex;
		private int misrecognitions;
		private int misrecognitionsTest;
		private int bestMisrecognitionsTest = int.MaxValue;
		private bool calculateTestMSE = true;

		private int learningRateIndex;
		private int learningRateEpochs;
		
		private TrainingPageViewModel learningPageViewModel = null;
		private TestingPageViewModel testingPageViewModel = null;
		private CalculatePageViewModel calculatePageViewModel = null;
		private DesignPageViewModel designPageViewModel = null;
		
		private CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
		private CommonSaveFileDialog saveFileDialog = new CommonSaveFileDialog();

		private bool needRefreshWeightsView = true;

		/// <summary>
		/// MainViewWindow constructor.
		/// </summary>
		public MainViewWindow()
		{
			InitializeComponent();
			
			// Checks if the system supports the TaskbarManager extensions (only Windows 7) so we can also show progress in the taskbar icon. 
			if (TaskbarManager.IsPlatformSupported)
				 taskbarManager = TaskbarManager.Instance;
			
			// Assign and create the DataProvider and default network to the global pageViewModel that acts as the DataContext in our MainView.  
			pageViewModel = new PageViewModel(new DataProvider(), InitializeDefaultNeuralNetwork());
			pageViewModel.MaxDegreeOfParallelism = Environment.ProcessorCount; //-1
			MainView.DataContext = pageViewModel;
			pageViewModel.DataProvider.RaiseDataLoadedEvent += new EventHandler<DataProviderEventArgs>(HandleDataLoadedEvent);
			pageViewModel.DataProvider.RaiseDataProgressEvent += new EventHandler<DataProviderEventArgs>(HandleDataProgressEvent);
			pageViewModel.RequestClose += this.OnViewModelRequestClose;
			pageViewModel.PageChange += this.OnViewModelPageChange;
		}

		~MainViewWindow()
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
				pageViewModel.RequestClose -= this.OnViewModelRequestClose;
				pageViewModel.PageChange -= this.OnViewModelPageChange;
				
				if (workerTimer != null)
					workerTimer.Dispose();

				if (openFileDialog != null)
					openFileDialog.Dispose();

				if (saveFileDialog != null)
					saveFileDialog.Dispose();
			}
			// Free unmanaged objects
		}

		public void Dispose()
		{
			Dispose(true);
			// Ensure that the destructor is not called
			GC.SuppressFinalize(this);
		}

		private NeuralNetworks InitializeDefaultNeuralNetwork()
		{
			//NeuralNetworks network = new NeuralNetworks("Simard-6", 0.8D, LossFunctions.MeanSquareError, 0.1D);
			//network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
			//network.Layers.Add(new Layers(network, LayerTypes.ConvolutionalSubsampling, KernelTypes.Sigmoid, 6, 14, 14, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.ConvolutionalSubsampling, KernelTypes.Sigmoid, 50, 5, 5, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 100));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 10));
			//network.InitWeights();

			//NeuralNetworks network = new NeuralNetworks("Simard-16", 0.8D, LossFunctions.MeanSquareError, 0.1D);
			//network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
			//network.Layers.Add(new Layers(network, LayerTypes.ConvolutionalSubsampling, KernelTypes.Sigmoid, 16, 14, 14, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.ConvolutionalSubsampling, KernelTypes.Sigmoid, 64, 5, 5, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 196));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 10));
			//network.InitWeights();


            //NeuralNetworks network = new NeuralNetworks("MyNet-16", 0.8D, LossFunctions.MeanSquareError, 0.02D);
            //network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
            //network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 16, 28, 28, 5, 5));
            //network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 16, 14, 14, 2, 2));
            //List<bool> mapCombinations = new List<bool>(64 * 16) 
            //{
            //    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
            //    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
            //    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
            //    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
            //    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
            //    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
            //    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
            //    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
            //    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
            //    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
            //    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
            //    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
            //    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
            //    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
            //    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
            //    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true
            //};
            //network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 64, 10, 10, 5, 5, new Mappings(network, 2, mapCombinations)));
            //network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 64, 5, 5, 2, 2));
            //network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 196, 1, 1, 5, 5));
            //network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 10));
            //network.InitWeights();


            NeuralNetworks network = new NeuralNetworks("LeNet-5", 0.8D, LossFunctions.MeanSquareError, 0.02D);
            network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
            network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 6, 28, 28, 5, 5));
            network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 6, 14, 14, 2, 2));
            List<bool> mapCombinations = new List<bool>(16 * 6) 
			{
			    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true
			};
            network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 16, 10, 10, 5, 5, new Mappings(network, 2, mapCombinations)));
            network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 16, 5, 5, 2, 2));
            network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 120, 1, 1, 5, 5));
            //network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 1, 7, 12));
            //network.Layers.Add(new Layers(network, LayerTypes.RBF, KernelTypes.Gaussian, 10));
            network.Layers.Add (new Layers (network, LayerTypes.FullyConnected , KernelTypes.Sigmoid, 10));
            network.InitWeights();
            //network.Layers.Last().LockedWeights = true;

			//NeuralNetworks network = new NeuralNetworks("MyNet-32", 0.8D, LossFunctions.MeanSquareError, 0.02D);
			//network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 32, 28, 28, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 32, 14, 14, 2, 2));
			//List<bool> mapCombinations = new List<bool>(128 * 32)
			//{
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true
			//};
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 128, 10, 10, 5, 5, new Mappings(network, 2, mapCombinations)));
			//network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 128, 5, 5, 2, 2));
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 256, 1, 1, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 10));
			//network.InitWeights();

			//NeuralNetworks network = new NeuralNetworks("MyNet-64", 0.8D, LossFunctions.MeanSquareError, 0.02D);
			//network.Layers.Add(new Layers(network, LayerTypes.Input, 1, 32, 32));
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 64, 28, 28, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 64, 14, 14, 2, 2));
			//List<bool> mapCombinations = new List<bool>(128 * 64)
			//{
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true,
			//    false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  false, true,
			//    false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true, false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,  true,
			//    true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true, true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,  true,
			//    true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true, true,  true,  false, false, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, true,
			//    true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true, true,  true,  true,  false, false, false, true,  true,  true,  false, false, true,  false, true,  true,  true,
			//    false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true, false, true,  true,  true,  false, false, true,  true,  true,  true,  false, false, true,  false, true,  true
			//};
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 128, 10, 10, 5, 5, new Mappings(network, 2, mapCombinations)));
			//network.Layers.Add(new Layers(network, LayerTypes.Subsampling, KernelTypes.AveragePooling, 128, 5, 5, 2, 2));
			//network.Layers.Add(new Layers(network, LayerTypes.Convolutional, KernelTypes.Sigmoid, 512, 1, 1, 5, 5));
			//network.Layers.Add(new Layers(network, LayerTypes.FullyConnected, KernelTypes.Sigmoid, 10));
			//network.InitWeights();
			

			return network;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			MainView.PageViews.IsEnabled = false;
			bool MNISTDataFilesAvailable = false;

			// Check if the required directories exists otherwise create them.
			if (!Directory.Exists(MNISTDirectory))
				Directory.CreateDirectory(MNISTDirectory);
            if (!Directory.Exists(TrainingSchemasDirectory))
                Directory.CreateDirectory(TrainingSchemasDirectory);

			// Check if the MNIST Dataset is available otherwise download the required files
			if (!File.Exists(MNISTDirectory + @"\t10k-labels-idx1-ubyte") || !File.Exists(MNISTDirectory + @"\train-labels-idx1-ubyte") ||
				!File.Exists(MNISTDirectory + @"\t10k-images-idx3-ubyte") || !File.Exists(MNISTDirectory + @"\train-images-idx3-ubyte"))
			{
				if (NetworkListManager.IsConnectedToInternet)
				{
					DownloadMNIST dialog = new DownloadMNIST();
					dialog.Path = MNISTDirectory;
					dialog.ShowDialog();
					MNISTDataFilesAvailable = dialog.AllDownloaded;
				}
				else
					InformationDialog.Show(this, "No Internet connection available to download the MNIST dataset.", "Application will exit", "Information");
			}
			else
				MNISTDataFilesAvailable = true;


			if (MNISTDataFilesAvailable)
			{
				Task.Factory.StartNew(() => pageViewModel.DataProvider.LoadDataFromFile(MNISTDirectory, 32, 32));

				OnViewModelPageChange(this, null);

				this.Title = "Convolutional Neural Network MNIST Workbench - " + pageViewModel.NeuralNetwork.Name;
			}
			else
				Application.Current.Shutdown(0);
		}

		private void OnViewModelRequestClose(object sender, EventArgs e)
		{
			Debug.WriteLine("OnViewModelRequestClose");
		}

		private void OnViewModelPageChange(object sender, EventArgs e)
		{
			Debug.WriteLine("OnViewModelPageChange"+ ": " + pageViewModel.CurrentPage.DisplayName.ToString());

			switch (pageViewModel.CurrentPage.DisplayName)
			{
				case "Training":
					if (learningPageViewModel == null)
					{
						learningPageViewModel = MainView.PageContentControl.Content as TrainingPageViewModel;
						learningPageViewModel.Start += this.OnViewModelStart;
						learningPageViewModel.Stop += this.OnViewModelStop;
                        learningPageViewModel.Editor += this.OnViewModelEditor;
					}

					(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;
					(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = false;
                    (MainView.DataContext as PageViewModel).CommandToolBar[2].IsEnabled = true;
					break;

				case "Testing":
					if (testingPageViewModel == null)
					{
						testingPageViewModel = MainView.PageContentControl.Content as TestingPageViewModel;
						testingPageViewModel.Start += this.OnViewModelStart;
						testingPageViewModel.Stop += this.OnViewModelStop;
					}

					(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;
					(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = false;

					if (testingPageViewModel.SelectedTabIndex == -1)
						testingPageViewModel.SelectedTabIndex = 0;
					break;

				case "Calculate":
					if (calculatePageViewModel == null)
					{
						calculatePageViewModel = MainView.PageContentControl.Content as CalculatePageViewModel;
						calculatePageViewModel.Calculate  += this.OnViewModelCalculate;
					}

					(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;

					if (calculatePageViewModel.SelectedTabIndex == -1)
						calculatePageViewModel.SelectedTabIndex = 0;
					break;

				case "Design":
					if (designPageViewModel == null)
						designPageViewModel = MainView.PageContentControl.Content as DesignPageViewModel;

					if (needRefreshWeightsView)
						RefreshWeightsView();

					if (designPageViewModel.SelectedTabIndex == -1)
						designPageViewModel.SelectedTabIndex = 0;
					break;
			}
		}

		private void RefreshWeightsView()
		{
			if (designPageViewModel.TabItems != null)
				designPageViewModel.TabItems.Clear();
			else
				designPageViewModel.TabItems = new ObservableCollection<TabItem>();

			foreach (Layers layer in pageViewModel.NeuralNetwork.Layers)
			{
				if (layer.LayerType == LayerTypes.Convolutional || layer.LayerType == LayerTypes.ConvolutionalSubsampling)
				{
					TabItem tb = new TabItem();
					tb.BeginInit();
					tb.UseLayoutRounding = true;
					tb.SnapsToDevicePixels = true;
					tb.Header = layer.LayerIndex.ToString(); 
					tb.Content = layer.GetMappedLayerWeights();
					tb.EndInit();
					designPageViewModel.TabItems.Add(tb);
				}
			}

			needRefreshWeightsView = false;
		}

		private void OnViewModelStart(object sender, EventArgs e)
		{
			Debug.WriteLine("OnViewModelStart");

			if (pageViewModel.CurrentContext == ContextTypes.Training)
			{
				if (trainingStopped)
				{
					//pageViewModel.NeuralNetwork.SetStandardLearingRates();
					//StartTraining();

					TrainingParameters dialog = new TrainingParameters();
					dialog.Data.Rate = 0.00085D;
					dialog.Data.Epochs = 34;
					dialog.Data.DecayFactor = 0.65;
					dialog.Data.MinimumRate = 0.000001D;
					//dialog.Data.DecayFactor = 0.794183335D;
					//dialog.Data.MinimumRate = 0.00005D;
					dialog.Data.DecayAfterEpochs = 2;
					dialog.Data.WeightSaveTreshold = 0.82D;
					dialog.Data.Distorted = true;
					dialog.Data.SameDistortionsForNEpochs = 1;
					dialog.Data.SeverityFactor = 1D;
					dialog.Data.MaxScaling = 15D;
					dialog.Data.MaxRotation = 15D;
					dialog.Data.ElasticSigma = 8.0D;
					dialog.Data.ElasticScaling = 0.5D;
					dialog.Owner = this;
					dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    dialog.pageViewModel = pageViewModel;
                    if (dialog.ShowDialog() ?? false)
                    {
                        StartTraining();
                    }
				}
			}
			else
			{
				if (testingStopped)
					StartTesting();
			}
		}

		private void OnViewModelStop(object sender, EventArgs e)
		{
			Debug.WriteLine("OnViewModelStop");

			if (pageViewModel.CurrentContext == ContextTypes.Training)
				StopTraining();
			else
			{
				StopTesting();
			}
		}

		private void OnViewModelCalculate(object sender, EventArgs e)
		{
			Debug.WriteLine("OnViewModelCalculate");
			Calculation calculate = Calculate;
			calculatePageViewModel.IsValid = false;
			(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = false;
			Application.Current.Dispatcher.BeginInvoke(calculate, DispatcherPriority.ApplicationIdle);
		}

        private void OnViewModelEditor(object sender, EventArgs e)
        {
            Debug.WriteLine("OnViewModelEditor");

            if (pageViewModel.CurrentContext == ContextTypes.Training)
            {
                if (trainingStopped)
                {
                    TrainingParametersEditor dialog = new TrainingParametersEditor();
                    dialog.Path = TrainingSchemasDirectory;
                    dialog.Data = new TrainingRate(true);
                    dialog.Owner = this;
                    dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    dialog.pageViewModel = pageViewModel;
                    if (dialog.ShowDialog() ?? false)
                    {
                        StartTraining();
                    }
                }
                else
                {
                    TrainingParametersEditor dialog = new TrainingParametersEditor();
                    dialog.Path = TrainingSchemasDirectory;
                    dialog.Data = new TrainingRate(true);
                    dialog.buttonTrain.IsEnabled = false;
                    dialog.Owner = this;
                    dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    dialog.pageViewModel = pageViewModel;
                    dialog.ShowDialog();
                }

            }
        }

		private void HandleDataProgressEvent(object sender, DataProviderEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Wait;
			if (TaskbarManager.IsPlatformSupported)
			{
				taskbarManager.SetProgressState(TaskbarProgressBarState.Normal);
				taskbarManager.SetProgressValue(e.Result, 100);
			}
			MainView.ProgressBar.Minimum = 0;
			MainView.ProgressBar.Maximum = 100;
			MainView.ProgressBar.Value = e.Result;
			MainView.Status.Text = e.Message;
			ShowProgressTime(e.Time);
			
			Trace.WriteLine("MainWindow received this message: " + e.Message);
		}

		private void HandleDataLoadedEvent(object sender, DataProviderEventArgs e)
		{
			pageViewModel.DataProvider.RaiseDataProgressEvent -= HandleDataProgressEvent;
			pageViewModel.DataProvider.RaiseDataLoadedEvent -= HandleDataLoadedEvent;

			if (TaskbarManager.IsPlatformSupported)
				taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);
			MainView.ProgressBar.Value = 0;
			MainView.Status.Text = "Ready";
			MainView.Time.Text = "00:00:00";

			Mouse.OverrideCursor = null;
			MainView.PageViews.IsEnabled = true;

			Trace.WriteLine("MainWindow received this message: " + e.Message);
		}

		private void ShowProgressTime(TimeSpan timeSpan)
		{
			string localTime;

			LocalTime.Length = 0;
			LocalTime.AppendFormat("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			localTime = LocalTime.ToString();
			MainView.Time.Text = localTime;
		}
		
		private void ShowElapsedTime(DateTime startTime)
		{
			string localTime;
			TimeSpan timeSpan = DateTime.Now.Subtract(startTime);
			LocalTime.Length = 0;
			LocalTime.AppendFormat("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
			localTime = LocalTime.ToString();
			MainView.Time.Text = localTime;
		}

		public BitmapSource ShowInputPattern(int patternIndex, bool trainingSet, bool distorted)
		{
			// Define parameters used to create the BitmapSource.
			PixelFormat pf = PixelFormats.Gray8;
			int rawStride = (pageViewModel.DataProvider.PatternWidth * pf.BitsPerPixel + 7) / 8;
			byte[] rawImage = new byte[rawStride * pageViewModel.DataProvider.PatternHeight];

			DoubleImageData pattern;

			if (trainingSet)
				pattern = pageViewModel.DataProvider.PreparedTrainingPatterns[patternIndex];
			else
				pattern = pageViewModel.DataProvider.TestingPatterns[patternIndex];

			if (pageViewModel.DataProvider.FlipGrayscale)
			{
				for (int i = 0; i < pageViewModel.DataProvider.PatternSize; ++i)
				{
					rawImage[i] = (byte)((pattern.Image[i] + 1) * pageViewModel.DataProvider.Divisor);
				}
			}
			else
			{
				for (int i = 0; i < pageViewModel.DataProvider.PatternSize; ++i)
				{
					rawImage[i] = (byte)(255 - ((pattern.Image[i] + 1) * pageViewModel.DataProvider.Divisor));
				}
			}

			BitmapSource output = BitmapSource.Create(pageViewModel.DataProvider.PatternWidth, pageViewModel.DataProvider.PatternHeight, 96, 96, pf, null, rawImage, rawStride);
			output.Freeze();
			
			return (output);
		}

		private void TrainingProgress()
		{
			StringBuilder learningProgressText = new StringBuilder();
			
			ShowElapsedTime(start);

			learningPageViewModel.ProgressImage = null;
			int index = curIndex;

			taskbarManager.SetProgressState(TaskbarProgressBarState.Normal);

			switch (learningPageViewModel.CurrentState)
			{
				case TrainingStates.PreparingData:
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(curIndex, pageViewModel.DataProvider.TrainingPatternsCount);
					MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TrainingPatternsCount;
					MainView.ProgressBar.Value = index;
					learningProgressText.Length = 0;
					learningProgressText.Append("Preparing Training Data...");
					learningPageViewModel.ProgressText = learningProgressText.ToString();
					learningPageViewModel.ProgressImageNumber = String.Empty;
					break;

				case TrainingStates.CalculatingHessian:
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(index, 500);
					MainView.ProgressBar.Maximum = 500;
					MainView.ProgressBar.Value = index;
					learningProgressText.Length = 0;
					learningProgressText.Append("Calculating Pseudo-Hessian...");
					learningPageViewModel.ProgressText = learningProgressText.ToString();
					learningPageViewModel.ProgressImageNumber = String.Empty;
					break;

				case TrainingStates.Training:
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(curIndex, pageViewModel.DataProvider.TrainingPatternsCount);
					MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TrainingPatternsCount;
					MainView.ProgressBar.Value = index;
					learningProgressText.Length = 0;
					learningProgressText.AppendFormat(((pageViewModel.NeuralNetwork.TrainingRates[learningRateIndex].Distorted) ? ("Distorted") : ("Not distorted")) + "\nEpoch:\t\t{0}\nPattern index:\t{1}\nTraining rate:\t{2:N10}\nAverage MSE:\t{3:N10}\nError percentage:\t{4:N10}\nMisrecognitions:\t{5}", curEpoch, index, pageViewModel.NeuralNetwork.TrainingRate.Rate, avgMSE, ((double)misrecognitions / ((double)index / 100D)), misrecognitions);
					learningPageViewModel.ProgressText = learningProgressText.ToString();
					learningPageViewModel.ProgressImage = ShowInputPattern(pageViewModel.DataProvider.RandomTrainingPattern[index], true, pageViewModel.NeuralNetwork.TrainingRates[learningRateIndex].Distorted);
					learningPageViewModel.ProgressImage.Freeze();
					learningPageViewModel.ProgressImageNumber = pageViewModel.DataProvider.PreparedTrainingPatterns[pageViewModel.DataProvider.RandomTrainingPattern[index]].Label.ToString();
					break;

				case TrainingStates.CalculatingTestError:
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(curIndex, pageViewModel.DataProvider.TestingPatternsCount);
					MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TestingPatternsCount;
					MainView.ProgressBar.Value = index;
					learningProgressText.Length = 0;
					learningProgressText.Append("Calculating Test Error...");
					learningProgressText.AppendFormat("\n\nPattern index:\t{0}\nAverage MSE:\t{1:N10}\nError percentage:\t{2:N10}\nMisrecognitions:\t{3}", index, avgTestMSE, ((double)misrecognitionsTest / ((double)index / 100D)), misrecognitionsTest);
					learningPageViewModel.ProgressText = learningProgressText.ToString();
					learningPageViewModel.ProgressImageNumber = String.Empty;
					break;

				case TrainingStates.SavingWaits:
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);
					MainView.ProgressBar.Value = 0;
					learningProgressText.Length = 0;
					learningProgressText.Append("Saving weights...");
					learningPageViewModel.ProgressText = learningProgressText.ToString();
					learningPageViewModel.ProgressImageNumber = String.Empty;
					break;

				case TrainingStates.NewEpoch:
					learningPageViewModel.ProgressImageNumber = String.Empty;
					TimeSpan span = DateTime.Now.Subtract(startSpan);
                    //Swordfish.WPF.Charts.ChartUtilities.AddTestLines(learningPageViewModel.Chart);
                    TrainingResult trainingResult = new TrainingResult(curEpoch, pageViewModel.NeuralNetwork.TrainingRate.Rate, pageViewModel.NeuralNetwork.TrainingRate.Distorted, avgMSE, misrecognitions, ((double)misrecognitions / ((double)600)), avgTestMSE, misrecognitionsTest, ((double)misrecognitionsTest / ((double)100)), new TimeSpan(span.Hours, span.Minutes, span.Seconds));
                    learningPageViewModel.DrawChart(trainingResult);//绘出误差曲线等
                    learningPageViewModel.TrainingResultCollection.Add(trainingResult);
					learningPageViewModel.CurrentState = TrainingStates.Idle;
					startSpan = DateTime.Now;
					break;
			}

			if (trainingStopped)
			{
				learningPageViewModel.ProgressImageNumber = String.Empty;
				if (TaskbarManager.IsPlatformSupported)
					taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);
				trainingStopped = false;
				StopTraining();
			}
		}

		private void TestingProgress()
		{
			StringBuilder testingProgressText = new StringBuilder();

			ShowElapsedTime(start);

			testingPageViewModel.ProgressImage = null;
			
			int index = curIndex;
			
			switch (testingPageViewModel.CurrentState)
			{
				case TestingStates.PreparingData:
					if (TaskbarManager.IsPlatformSupported)
					{
						taskbarManager.SetProgressState(TaskbarProgressBarState.Normal);
						taskbarManager.SetProgressValue(curIndex, pageViewModel.DataProvider.TrainingPatternsCount, this);
					}
					MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TrainingPatternsCount;
					MainView.ProgressBar.Value = index;
					testingProgressText.Length = 0;
					testingProgressText.Append("Preparing Testing Data...");
					testingPageViewModel.ProgressText = testingProgressText.ToString();
					testingPageViewModel.ProgressImageNumber = String.Empty;
					break;

				case TestingStates.Testing:
					if (!testingPageViewModel.UseTrainingSet)
						MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TestingPatternsCount;
					else
						MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TrainingPatternsCount;

					
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(index, (int)MainView.ProgressBar.Maximum);
					MainView.ProgressBar.Value = index;
					testingProgressText.Length = 0;
					testingProgressText.AppendFormat(((testingPageViewModel.UseDistortions) ? ("Distorted") : ("Not distorted")) + "\nPattern index:\t{0}\nAverage MSE:\t{1:N10}\nError percentage:\t{2:N10}\nMisrecognitions:\t{3}", index, avgMSE, ((double)misrecognitions / ((double)index / 100D)), misrecognitions);
					testingPageViewModel.ProgressImage = ShowInputPattern(index, (MainView.PageContentControl.Content as TestingPageViewModel).UseTrainingSet, (MainView.PageContentControl.Content as TestingPageViewModel).UseDistortions);
					testingPageViewModel.ProgressImage.Freeze();
					if ((MainView.PageContentControl.Content as TestingPageViewModel).UseTrainingSet)
						testingPageViewModel.ProgressImageNumber = pageViewModel.DataProvider.PreparedTrainingPatterns[index].Label.ToString();
					else
						testingPageViewModel.ProgressImageNumber = pageViewModel.DataProvider.TestingPatterns[index].Label.ToString();
					testingPageViewModel.ProgressText = testingProgressText.ToString();
					break;

				case TestingStates.Idle:
					if (!testingPageViewModel.UseTrainingSet)
						MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TestingPatternsCount;
					else
						MainView.ProgressBar.Maximum = pageViewModel.DataProvider.TrainingPatternsCount;
					index = curIndex;
					if (TaskbarManager.IsPlatformSupported)
						taskbarManager.SetProgressValue(index, (int)MainView.ProgressBar.Maximum);
					MainView.ProgressBar.Value = index;
					testingPageViewModel.ProgressImageNumber = String.Empty;
					testingProgressText.Length = 0;
					testingProgressText.AppendFormat(((testingPageViewModel.UseDistortions) ? ("Distorted") : ("Not distorted")) + "\nPattern index:\t{0}\nAverage MSE:\t{1:N10}\nError percentage:\t{2:N10}\nMisrecognitions:\t{3}", index, avgMSE, ((double)misrecognitions / ((double)index / 100D)), misrecognitions);
					testingPageViewModel.ProgressText = testingProgressText.ToString();
					testingStopped = true;
					break;
			}

			if (testingStopped)
			{
				testingPageViewModel.ProgressImageNumber = String.Empty;

				if (TaskbarManager.IsPlatformSupported)
					taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);

				testingStopped = false;
				StopTesting();
			}
		}

		private void WorkerTimerElapsed(Object data, System.Timers.ElapsedEventArgs eventArgs)
		{
			System.Windows.Application.Current.Dispatcher.Invoke(workerProgress, System.Windows.Threading.DispatcherPriority.Render);
		}

		private void StartTraining()
		{
			if (trainingStopped == true)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = false;
                //(MainView.DataContext as PageViewModel).CommandToolBar[2].IsEnabled = false;
				learningPageViewModel.IsValid = false;

				trainingStopped = false;
				
				MainView.Status.Text = pageViewModel.CurrentContext.ToString() + "...";

				workerProgress = TrainingProgress;
				workerThread = new Thread(new ThreadStart(TrainingTask));
				workerThread.Name = pageViewModel.CurrentContext.ToString() + "patterns...";
				workerThread.IsBackground = true;

				start = DateTime.Now;
				startSpan = DateTime.Now;
				workerThread.Start();

				workerTimer = new System.Timers.Timer(1000);
				workerTimer.Elapsed += new ElapsedEventHandler(WorkerTimerElapsed);
				workerTimer.Start();

				(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = true;
				Mouse.OverrideCursor = null;
			}
		}

		private void StopTraining()
		{
			if (trainingStopped == false)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = false;
				
				trainingStopped = true;
				workerThread.Join();

				workerTimer.Stop();
				workerTimer.Elapsed -= new ElapsedEventHandler(WorkerTimerElapsed);
				workerTimer.Dispose();

				MainView.Status.Text = "Ready";
				learningPageViewModel.ProgressImageNumber = String.Empty;
				learningPageViewModel.ProgressImage  = null;
				MainView.ProgressBar.Value = 0;
				learningPageViewModel.ProgressText = String.Empty;
				MainView.Time.Text = "00:00:00";
				if (TaskbarManager.IsPlatformSupported)
					taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);

				(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;
                //(MainView.DataContext as PageViewModel).CommandToolBar[2].IsEnabled = true;
				learningPageViewModel.IsValid = true;
				Mouse.OverrideCursor = null;
			}
		}

		private void StartTesting()
		{
			if (testingStopped == true)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = false;
				
				testingPageViewModel.IsValid = false;

				MainView.Status.Text = pageViewModel.CurrentContext.ToString() + "...";
											  
				// Assign AddWrongItem delegate for filling the ConfusionMatrix
				addErrorItem = AddWrongItem;  

				testingPageViewModel.ConfusionDocument = String.Empty;
				testingPageViewModel.ConfusionMatrix = new ConfusionMatrix("ConfusionMatrix", pageViewModel.DataProvider);
				
				workerProgress = TestingProgress;
				workerThread = new Thread(new ThreadStart(TestingTask));
				workerThread.Name = pageViewModel.CurrentContext.ToString() + "patterns...";
				workerThread.IsBackground = true;

				testingStopped = false;
				start = DateTime.Now;
				workerThread.Start();

				workerTimer = new System.Timers.Timer(1000);
				workerTimer.Elapsed += new ElapsedEventHandler(WorkerTimerElapsed);
				workerTimer.Start();

				(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = true;
				Mouse.OverrideCursor = null;
			}
		}

		private void StopTesting()
		{
			if (testingStopped == false)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				(MainView.DataContext as PageViewModel).CommandToolBar[1].IsEnabled = false;

				workerTimer.Stop();
				workerTimer.Elapsed -= new ElapsedEventHandler(WorkerTimerElapsed);
				workerTimer.Dispose();

				testingStopped = true;
				workerThread.Join(2500);

				if (TaskbarManager.IsPlatformSupported)
					taskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);

				MainView.Status.Text = "Ready";
				MainView.ProgressBar.Value = 0;
				int total = 0;

				testingPageViewModel.ConfusionDocument = String.Empty;

				foreach (ConfusionItems wrong in testingPageViewModel.ConfusionMatrix)
				{
					if (wrong != null)
					{
						total += wrong.Items.Count;
						testingPageViewModel.ConfusionDocument += wrong.CorrectValue.ToString() + " (" + wrong.Items.Count.ToString() + " incorrect)" + "\r\n" + "=======================" + "\r\n";
						foreach (ConfusedItem wrongImage in wrong.Items)
						{
							if (wrongImage != null)
								testingPageViewModel.ConfusionDocument += wrongImage.PatternIndex.ToString("D4") + " interpreted as " + wrongImage.WrongValue.ToString() + "\r\n";
						}
					}
					testingPageViewModel.ConfusionDocument += "\r\n";
				}
				testingPageViewModel.ConfusionDocument += total.ToString() + " patterns incorrect";

				foreach (ConfusionItems wrong in testingPageViewModel.ConfusionMatrix)
				{
					if (wrong != null)
						wrong.GetConfusedItems();
				}
				testingPageViewModel.SelectedTabIndex = 0;

				(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;

				MainView.Time.Text = "00:00:00";
				
				testingPageViewModel.IsValid = true;
				Mouse.OverrideCursor = null;
			}
		}

		private void TrainingTask()
		{
			curEpoch = 0;
			learningRateIndex = 0;
			prevAvgMSE = 0D;
			pageViewModel.NeuralNetwork.TrainingRate = pageViewModel.NeuralNetwork.TrainingRates[learningRateIndex];
			learningRateEpochs = pageViewModel.NeuralNetwork.TrainingRates[learningRateIndex].Epochs;
			bestMisrecognitionsTest = (int)(pageViewModel.NeuralNetwork.TrainingRates[0].WeightSaveTreshold * 100);
			needRefreshWeightsView = true;

			int MaxEpochs = 0;

			foreach (TrainingRate rate in pageViewModel.NeuralNetwork.TrainingRates)
			{
				MaxEpochs += rate.Epochs;
			}

			while ((!trainingStopped) && (curEpoch < MaxEpochs))
			{
				if (curEpoch == learningRateEpochs)
				{
					learningRateIndex++;
					pageViewModel.NeuralNetwork.TrainingRate = pageViewModel.NeuralNetwork.TrainingRates[learningRateIndex];
					learningRateEpochs += pageViewModel.NeuralNetwork.TrainingRate.Epochs;
				}

				curEpoch++;

				//Prepare Training Patterns
				learningPageViewModel.CurrentState = TrainingStates.PreparingData;
				pageViewModel.DataProvider.PrepareTrainingData(pageViewModel.NeuralNetwork.TrainingRate, curEpoch, ref curIndex, ref trainingStopped);
				if (trainingStopped)
					break;

				//Compute Hessian on each new epoch
				learningPageViewModel.CurrentState = TrainingStates.CalculatingHessian;
				CalculateHessian(ref curIndex, ref trainingStopped);
				if (trainingStopped)
					break;

				//Training Patterns
				learningPageViewModel.CurrentState = TrainingStates.Training;
				LearnPattern(ref curIndex, ref trainingStopped);
				if (trainingStopped)
					break;

				if (calculateTestMSE)
				{
					learningPageViewModel.CurrentState = TrainingStates.CalculatingTestError;
					TestMSE(ref curIndex, ref trainingStopped);

					if (misrecognitionsTest <= bestMisrecognitionsTest)
					{
						bestMisrecognitionsTest = misrecognitionsTest;
						learningPageViewModel.CurrentState = TrainingStates.SavingWaits;
                        pageViewModel.NeuralNetwork.SaveWeights(ApplicationStorageDirectory + @"\" + pageViewModel.NeuralNetwork.Name + " (epoch " + curEpoch.ToString() + " - " + bestMisrecognitionsTest.ToString() + " errors).weights-xml-gz");
					}
				}
				if (trainingStopped)
					break;

				learningPageViewModel.CurrentState = TrainingStates.NewEpoch;
				prevAvgMSE = avgMSE;

				while (learningPageViewModel.CurrentState == TrainingStates.NewEpoch)
				{
					Thread.SpinWait(1);
					if (trainingStopped)
						break;
				}
			}
			trainingStopped = true;
		}

		private void TestingTask()
		{
			avgMSE = 0;
			misrecognitions = 0;
			curIndex = 0;

			testingPageViewModel.CurrentState = TestingStates.PreparingData;

			TrainingRate rate = new TrainingRate(testingPageViewModel.UseDistortions);
			if (testingPageViewModel.UseDistortions)
			{
				rate.SeverityFactor = testingPageViewModel.SeverityFactor;
				rate.MaxScaling = testingPageViewModel.MaxScaling;
				rate.MaxRotation = testingPageViewModel.MaxRotation;
				rate.ElasticScaling = testingPageViewModel.ElasticScaling;
				rate.ElasticSigma = testingPageViewModel.ElasticSigma;
			}

			pageViewModel.DataProvider.PrepareTrainingData(rate, 1, ref curIndex, ref testingStopped);

			testingPageViewModel.CurrentState = TestingStates.Testing;
			TestPattern(ref curIndex, ref testingStopped);

			testingPageViewModel.CurrentState = TestingStates.Idle;

			while (testingPageViewModel.CurrentState == TestingStates.Idle)
			{
				Thread.SpinWait(1);
				if (testingStopped)
					break;
			}

			testingStopped = true;
		}

		private int SetTrainingPattern(int index)
		{
			Layers inputLayer = pageViewModel.NeuralNetwork.Layers.First();

			for (int i = 0; i < pageViewModel.DataProvider.PatternSize; i++)
			{
				inputLayer.Neurons[i].Output = pageViewModel.DataProvider.PreparedTrainingPatterns[pageViewModel.DataProvider.RandomTrainingPattern[index]].Image[i];
			}

			return (pageViewModel.DataProvider.PreparedTrainingPatterns[pageViewModel.DataProvider.RandomTrainingPattern[index]].Label);
		}

		private int SetTestingPattern(int index)
		{
			if (pageViewModel.CurrentContext ==  ContextTypes.Training)
			{
				// Training PageView
				Layers inputLayer = pageViewModel.NeuralNetwork.Layers.First();

				for (int i = 0; i < pageViewModel.DataProvider.PatternSize; i++)
				{
					inputLayer.Neurons[i].Output = pageViewModel.DataProvider.TestingPatterns[index].Image[i];
				}

				return (pageViewModel.DataProvider.TestingPatterns[index].Label);
			}
			else
			{
				// Testing PageView
				DoubleImageData pattern;

				if (!testingPageViewModel.UseTrainingSet)
				{
					if (testingPageViewModel.UseDistortions)
					   pattern = new DoubleImageData(pageViewModel.DataProvider.TestingPatterns[index].Label, pageViewModel.DataProvider.GetDistortedPattern(index, false));
					else
						pattern = pageViewModel.DataProvider.TestingPatterns[index];
				}
				else
				{
					if (testingPageViewModel.UseDistortions)
						pattern = new DoubleImageData(pageViewModel.DataProvider.TrainingPatterns[index].Label, pageViewModel.DataProvider.GetDistortedPattern(index, true));
					else
						pattern = pageViewModel.DataProvider.TrainingPatterns[index];
				}

				Layers inputLayer = pageViewModel.NeuralNetwork.Layers.First();
				for (int i = 0; i < pageViewModel.DataProvider.PatternSize; i++)
				{
					inputLayer.Neurons[i].Output = pattern.Image[i];
				}

				return (pattern.Label);
			}
		}

		private double GetPatternMSE(int desiredPatternClass)
		{
			Layers outputLayer = pageViewModel.NeuralNetwork.Layers.Last();
			double patternMSE = 0D;

			for (int i=0; i < outputLayer.NeuronCount; i++)
			{
				if (i == desiredPatternClass)
					patternMSE += Math.Pow(outputLayer.Neurons[i].Output - pageViewModel.NeuralNetwork.TrainToValue, 2);
				else
					patternMSE += Math.Pow(outputLayer.Neurons[i].Output + pageViewModel.NeuralNetwork.TrainToValue, 2);
			}

			patternMSE *= 0.5D;

			return (patternMSE);
		}

		private int Recognised()
		{
			Layers outputLayer = pageViewModel.NeuralNetwork.Layers.Last();
			double maxValue = double.MinValue;
			int bestIndex = 0;
			for (int i = 0; i < outputLayer.NeuronCount; i++)
			{
				if (outputLayer.Neurons[i].Output > maxValue)
				{
					maxValue = outputLayer.Neurons[i].Output;
					bestIndex = i;
				}
			}

			return (bestIndex);
		}

		private void LearnPattern(ref int index, ref bool stop)
		{
			int desiredPatternClass;
			double patternMSE;
			int bestIndex;
			
			double totMSE = 0D;
			avgMSE = 0;
			pageViewModel.DataProvider.ScrambleInputPatterns();
			misrecognitions = 0;
			
			for (index = 0; index < pageViewModel.DataProvider.TrainingPatternsCount; index++)
			{
				desiredPatternClass = SetTrainingPattern(index);
				pageViewModel.NeuralNetwork.Calculate();
				patternMSE = GetPatternMSE(desiredPatternClass);
				totMSE += patternMSE;
				avgMSE = totMSE / (index+1);

				bestIndex = Recognised();
				if (desiredPatternClass != bestIndex)
					misrecognitions++;

				if ((patternMSE > (prevAvgMSE * 0.1D)) || (bestIndex != desiredPatternClass))
					pageViewModel.NeuralNetwork.Backpropagate(desiredPatternClass);

				if (stop)
					break;
			}
		}

		private void TestPattern(ref int index, ref bool stop)
		{
			int end;

			if (testingPageViewModel.UseTrainingSet)
				end = pageViewModel.DataProvider.TrainingPatternsCount;
			else
				end = pageViewModel.DataProvider.TestingPatternsCount;
		   
			int desiredPatternClass = 0;
			int bestIndex = 0;
			double totMSE = 0D;
			avgMSE = 0D;

			for (index = 0; index < end; index++)
			{
				if (stop)
					break;

				desiredPatternClass = SetTestingPattern(index);

				pageViewModel.NeuralNetwork.Calculate();

				totMSE += GetPatternMSE(desiredPatternClass);
				avgMSE = totMSE / (index + 1);
				
				bestIndex = Recognised();

				if (bestIndex != desiredPatternClass)
				{
					misrecognitions++;
					Dispatcher.Invoke(addErrorItem, System.Windows.Threading.DispatcherPriority.Send, new object[] { desiredPatternClass, index, bestIndex });
			   }
				
			}
		}

		private void AddWrongItem(int desiredPatternClass, int patternClass, int bestIndex)
		{
			testingPageViewModel.ConfusionMatrix[desiredPatternClass].Items.Add(new ConfusedItem(patternClass, bestIndex, !testingPageViewModel.UseTrainingSet, pageViewModel.DataProvider));
		}

		private void CalculateHessian(ref int index, ref bool stop)
		{
			int end = pageViewModel.DataProvider.TrainingPatternsCount / 120;

			pageViewModel.DataProvider.ScrambleInputPatterns();
			pageViewModel.NeuralNetwork.EraseHessianInformation();
			for (index = 0; index < end; index++)
			{
				SetTrainingPattern(index);
				pageViewModel.NeuralNetwork.Calculate();
				pageViewModel.NeuralNetwork.BackpropagateSecondDerivates();

				if (stop)
				{
					pageViewModel.NeuralNetwork.DivideHessianInformationBy((double)(index+1));
					break;
				}
			}

			if (!stop)
				pageViewModel.NeuralNetwork.DivideHessianInformationBy((double)end);
		}

		private void TestMSE(ref int index, ref bool stop)
		{
			int desiredPatternClass;

			double totTestMSE = 0;
			avgTestMSE = 0D;
			misrecognitionsTest = 0;
			for (index = 0; index < pageViewModel.DataProvider.TestingPatternsCount; index++)
			{
				desiredPatternClass = SetTestingPattern(index);
				pageViewModel.NeuralNetwork.Calculate();
				totTestMSE += GetPatternMSE(desiredPatternClass);
				avgTestMSE = totTestMSE / (index+1);
				if (desiredPatternClass != Recognised())
					misrecognitionsTest++;

				if (stop)
					break;
			}
		}

		private void Calculate()
		{
			(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = false;
			calculatePageViewModel.IsValid = false;
			MainView.Status.Text = "Calculating...";

			Mouse.OverrideCursor = Cursors.Wait;
			DoubleImageData pattern;

			if (calculatePageViewModel.UseTrainingSet)
			{
				if (calculatePageViewModel.UseDistortions)
				{
					pageViewModel.DataProvider.SeverityFactor = calculatePageViewModel.SeverityFactor;
					pageViewModel.DataProvider.MaxRotation = calculatePageViewModel.MaxRotation;
					pageViewModel.DataProvider.MaxScaling = calculatePageViewModel.MaxScaling;
					pageViewModel.DataProvider.ElasticScaling = calculatePageViewModel.ElasticScaling;
					pageViewModel.DataProvider.ElasticSigma = calculatePageViewModel.ElasticSigma;
					pageViewModel.DataProvider.CalculateGaussianKernel();

					pattern = new DoubleImageData(pageViewModel.DataProvider.TrainingPatterns[calculatePageViewModel.PatternIndex].Label, pageViewModel.DataProvider.GetDistortedPattern(calculatePageViewModel.PatternIndex, true));
				}
				else
					pattern = pageViewModel.DataProvider.TrainingPatterns[calculatePageViewModel.PatternIndex];
			}
			else
			{
				if (calculatePageViewModel.UseDistortions)
				{
					pageViewModel.DataProvider.SeverityFactor = calculatePageViewModel.SeverityFactor;
					pageViewModel.DataProvider.MaxRotation = calculatePageViewModel.MaxRotation;
					pageViewModel.DataProvider.MaxScaling = calculatePageViewModel.MaxScaling;
					pageViewModel.DataProvider.ElasticScaling = calculatePageViewModel.ElasticScaling;
					pageViewModel.DataProvider.ElasticSigma = calculatePageViewModel.ElasticSigma;
					pageViewModel.DataProvider.CalculateGaussianKernel();

					pattern = new DoubleImageData(pageViewModel.DataProvider.TestingPatterns[calculatePageViewModel.PatternIndex].Label, pageViewModel.DataProvider.GetDistortedPattern(calculatePageViewModel.PatternIndex, false));
				}
				else
					pattern = pageViewModel.DataProvider.TestingPatterns[calculatePageViewModel.PatternIndex];
			}
			
			Layers firstLayer = pageViewModel.NeuralNetwork.Layers.First();
			for (int i = 0; i < pageViewModel.DataProvider.PatternSize; ++i)
			{
				//pattern[i] = ((pattern[i] * 0.6375D) + 0.5375D); // normalization (-0.1 --> 1.175)
				firstLayer.Neurons[i].Output = pattern.Image[i];
			}

			string result = String.Empty;
			result += "Input: " + pattern.Label.ToString() + "\r\n";

			int bestIndex = 0;
			System.Diagnostics.Stopwatch timer = new Stopwatch();
			timer.Restart();
			pageViewModel.NeuralNetwork.Calculate();
			bestIndex = Recognised();
			timer.Stop();
			
			result += "\r\n";
			Layers lastLayer = pageViewModel.NeuralNetwork.Layers.Last();
			for (int i = 0; i < lastLayer.NeuronCount; i++)
			{
				result += i.ToString() + " ==> " + lastLayer.Neurons[i].Output.ToString("N17") + "\r\n";
			}
			result += "\r\n";

			double patternMSE = GetPatternMSE(pattern.Label);
			result += "Pattern MSE: " + patternMSE.ToString("N17") + "\r\n";

			result += "Highest prob: " + bestIndex.ToString() + "\r\n";
			if (bestIndex == pattern.Label)
				result += "Correctly recognized";
			else
				result += "Not correctly recognized";

			result += "\r\nTime: " + timer.ElapsedMilliseconds.ToString() + " ms.";


			calculatePageViewModel.Result = result;

			calculatePageViewModel.TabItems = new ObservableCollection<TabItem>();

			foreach (Layers layer in pageViewModel.NeuralNetwork.Layers)
			{
				if (layer.UseMapInfo)
				{
					TabItem tb = new TabItem();
					tb.BeginInit();
					tb.UseLayoutRounding = true;
					tb.SnapsToDevicePixels = true;
					tb.Header = layer.LayerIndex.ToString();
					tb.Content = layer.GetMappedLayerOutputs();
					tb.EndInit();
					calculatePageViewModel.TabItems.Add(tb);
				}
			}

			Mouse.OverrideCursor = null;
		   
			MainView.Status.Text = "Ready";
			calculatePageViewModel.IsValid = true;
			(MainView.DataContext as PageViewModel).CommandToolBar[0].IsEnabled = true;
		}

		#region "Menu handling not yet implemented"
		void PrintCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			//String command, targetobj;
			//command = ((RoutedCommand)e.Command).Name;
			//targetobj = ((FrameworkElement)target).Name;
			//TaskDialog.Show("The " + command + " command has been invoked on target object " + targetobj);
		}

		void PrintCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		void PrintPreviewCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			//String command, targetobj;
			//command = ((RoutedCommand)e.Command).Name;
			//targetobj = ((FrameworkElement)target).Name;
			//TaskDialog.Show("The " + command + " command has been invoked on target object " + targetobj);
		}

		void PrintPreviewCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		void PageSetupCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			//String command, targetobj;
			//command = ((RoutedCommand)e.Command).Name;
			//targetobj = ((FrameworkElement)target).Name;
			//TaskDialog.Show("The " + command + " command has been invoked on target object " + targetobj);
		}

		void PageSetupCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}
		#endregion

		void CutCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			(e.Source as TextBox).Cut();
			e.Handled = true;
		}

		void CutCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source.GetType() == typeof(TextBox))
				e.CanExecute = (e.Source as TextBox).SelectionLength > 0;
			else
				e.CanExecute = false;
		}

		void CopyCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			(e.Source as TextBox).Copy();
			e.Handled = true;
		}

		void CopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source.GetType() == typeof(TextBox))
				e.CanExecute = (e.Source as TextBox).SelectionLength > 0;
			else
				e.CanExecute = false;
		}

		void PasteCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			(e.Source as TextBox).Paste();
			e.Handled = true;
		}

		void PasteCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		void SelectAllCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			(e.Source as TextBox).SelectAll ();
			e.Handled = true;
		}

		void SelectAllCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source.GetType() == typeof(TextBox))
				e.CanExecute = true;
			else
				e.CanExecute = false;
		}

		void UndoCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			e.Handled = (e.Source as TextBox).Undo();
		}

		void UndoCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source.GetType() == typeof(TextBox))
				e.CanExecute = (e.Source as TextBox).CanUndo;
			else
				e.CanExecute = false;
		}

		void RedoCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			e.Handled = (e.Source as TextBox).Redo();
		}

		void RedoCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source.GetType() == typeof(TextBox))
				e.CanExecute = (e.Source as TextBox).CanRedo;
			else
				e.CanExecute = false;
		}

		void HelpCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			//Word.Application wordApplication = new Word.Application() {Visible = true};
			//string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			//try
			//{
			//    wordApplication.Documents.Open(directoryName + @"\\Manual\\A Convolutional Neural Network Workbench.docx", ReadOnly: true);
			//}
			//catch
			//{
			//    //wordApplication.Quit(SaveChanges: false, OriginalFormat: null, RouteDocument: null);
			//}
		}

		void HelpCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		void AboutCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			Window aboutDialog = new About();
			aboutDialog.Owner = this;
			aboutDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
			aboutDialog.ShowDialog();
		}

		void AboutCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
		
		void ApplicationExitCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			Application.Current.MainWindow.Close();
		}

		void ApplicationExitCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
			openFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Weights Documents", "weights-xml-gz"));
			openFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Definition Documents", "definition-xml-gz"));
			openFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Network Definition and weights", "full-xml-gz"));
			
			openFileDialog.Title = "Load neural network definition or weights";
			openFileDialog.DefaultExtension = "weights-xml-gz";
			openFileDialog.AllowNonFileSystemItems = true;
            openFileDialog.InitialDirectory = ApplicationStorageDirectory + @"\";
			openFileDialog.EnsureFileExists = true;
			openFileDialog.EnsurePathExists = true;
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				Mouse.OverrideCursor = Cursors.Wait;
				string fileName = openFileDialog.FileAsShellObject.ParsingName;

				if (fileName.Contains(".definition-xml-gz"))
				{
					NeuralNetworks network = NeuralNetworks.Load(fileName, false);
					if (network == null)
						return;
					else
						pageViewModel.NeuralNetwork = network;

					Title = "Convolutional Neural Network MNIST Workbench - " + pageViewModel.NeuralNetwork.Name;
					needRefreshWeightsView = true;
					if (pageViewModel.CurrentContext == ContextTypes.Design)
						RefreshWeightsView();
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network definition is loaded.", "Information");
				}

				if (fileName.Contains(".full-xml-gz"))
				{
					NeuralNetworks network = NeuralNetworks.Load(fileName, true);
					if (network == null)
						return;
					else
						pageViewModel.NeuralNetwork = network;

                    Title = "Convolutional Neural Network MNIST Workbench - " + pageViewModel.NeuralNetwork.Name;
					needRefreshWeightsView = true;
					if (pageViewModel.CurrentContext == ContextTypes.Design)
						RefreshWeightsView();
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network definition and weights are loaded.", "Information");
				}

				if (fileName.Contains(pageViewModel.NeuralNetwork.Name) && fileName.Contains(".weights-xml-gz"))
				{
					pageViewModel.NeuralNetwork.LoadWeights(fileName);
					needRefreshWeightsView = true;
					if (pageViewModel.CurrentContext == ContextTypes.Design)
						RefreshWeightsView();
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network weights are loaded.", "Information");
				}
			}
			Mouse.OverrideCursor = null;
		}

		void SaveCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
            string filePath = ApplicationStorageDirectory + @"\";
			string fileName = pageViewModel.NeuralNetwork.Name;
			Mouse.OverrideCursor = Cursors.Wait;

			switch (pageViewModel.CurrentContext)
			{
				case ContextTypes.Training:
					fileName += ".weights-xml-gz";
					pageViewModel.NeuralNetwork.SaveWeights(filePath + fileName);
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network weights are saved.", "", "Information");
					break;

				case ContextTypes.Testing:
				case ContextTypes.Calculate:
				case ContextTypes.Design:
					fileName += ".definition-xml-gz";
					pageViewModel.NeuralNetwork.Save(filePath + fileName, false);
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network definition is saved.", "", "Information");
					break;
			}
		}

		void SaveAsCmdExecuted(object target, ExecutedRoutedEventArgs e)
		{
            saveFileDialog.InitialDirectory = ApplicationStorageDirectory;
			saveFileDialog.DefaultFileName = pageViewModel.NeuralNetwork.Name;
			saveFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Network Weights", "weights-xml-gz"));
			saveFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Network Definition", "definition-xml-gz"));
			saveFileDialog.Filters.Add(new CommonFileDialogFilter("Zipped Xml Network Definition and weights", "full-xml-gz"));
			
			saveFileDialog.DefaultExtension = "weights-xml-gz";
			saveFileDialog.AlwaysAppendDefaultExtension = true;
			saveFileDialog.AllowPropertyEditing = false;
			saveFileDialog.ShowPlacesList = true;
			saveFileDialog.CreatePrompt = false;
			saveFileDialog.OverwritePrompt = true;
			

			if (saveFileDialog.ShowDialog () == CommonFileDialogResult.Ok)
			{
				string fileName = saveFileDialog.FileAsShellObject.ParsingName;

				Mouse.OverrideCursor = Cursors.Wait;
				if (fileName.Contains(".definition-xml-gz"))
				{
					pageViewModel.NeuralNetwork.Save(fileName, false);
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network definition is saved.", "", "Information");
				}

				if (fileName.Contains(".full-xml-gz"))
				{
					pageViewModel.NeuralNetwork.Save(fileName, true);
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network definition and weights are saved.", "", "Information");
				}

				if (fileName.Contains(".weights-xml-gz"))
				{
					pageViewModel.NeuralNetwork.SaveWeights(fileName);
					Mouse.OverrideCursor = null;
					InformationDialog.Show(this, "Neural network weights are saved.", "", "Information");
				}
				Mouse.OverrideCursor = null;
			}
		}

        void FileCommands(ref CanExecuteRoutedEventArgs e)
        {
            switch (pageViewModel.CurrentContext)
            {
                case ContextTypes.Design:
                    e.CanExecute = designPageViewModel.IsValid;
                    break;

                case ContextTypes.Training:
                    e.CanExecute = learningPageViewModel.IsValid;
                    break;

                case ContextTypes.Testing:
                    e.CanExecute = testingPageViewModel.IsValid;
                    break;

                case ContextTypes.Calculate:
                    e.CanExecute = calculatePageViewModel.IsValid;
                    break;
            }
        }

		void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
            FileCommands(ref e);
		}

		void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
            FileCommands(ref e);
		}

		void SaveAsCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
            FileCommands(ref e);
		}
	}
}
