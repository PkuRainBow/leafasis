using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.Util;
using CNNWB.Common;
using Emgu.CV.Structure;
using System.Collections.Generic;


namespace CNNWB.Model
{
	public struct TrainingResult
	{
		public int Epoch { get; set; }
		public double TrainingRate { get; set; }
		public bool Distorted { get; set; }
		public double AvgTrainMSE { get; set; }
		public int TrainErrors { get; set; }
		public double TrainErrorPercentage { get; set; }
		public double AvgTestMSE { get; set; }
		public int TestErrors { get; set; }
		public double TestErrorPercentage { get; set; }
		public TimeSpan ElapsedTime { get; set; }

		public TrainingResult(int epoch, double rate, bool distorted, double avgTrainMSE, int trainErrors, double trainErrorPercentage, double avgTestMSE, int testErrors, double testErrorPercentage, TimeSpan elapsedTime) : this()
		{
			Epoch = epoch;
			TrainingRate = rate;
			Distorted = distorted;
			AvgTrainMSE = avgTrainMSE;
			TrainErrors = trainErrors;
			TrainErrorPercentage = trainErrorPercentage;
			AvgTestMSE = avgTestMSE;
			TestErrors = testErrors;
			TestErrorPercentage = testErrorPercentage;
			ElapsedTime = elapsedTime;
		}
	}

	public struct ByteImageData
	{
		public int Label { get; set; }
		public byte[] Image { get; set; }

		public ByteImageData(int label, byte[] image) : this()
		{
			Label = label;
			Image = image;
		}
	}

	public struct DoubleImageData
	{
		public int Label { get; set; }
		public double[] Image { get; set; }
		
		public DoubleImageData(int label, double[] image) : this()
		{
			Label = label;
			Image = image;
		}
	}

	public class DataProviderEventArgs : EventArgs
	{
		public int Result { get; private set; }
		public string Message { get; private set; }
		public TimeSpan Time { get; private set; }

		public DataProviderEventArgs(int result, string msg, TimeSpan time)
		{
			Result = result;
			Message = msg;
			Time = time;
		}
	}

	public class DataProvider : IDisposable 
	{
		public event EventHandler<DataProviderEventArgs> RaiseDataLoadedEvent = delegate { };
		public event EventHandler<DataProviderEventArgs> RaiseDataProgressEvent = delegate { };
		
		public bool FlipGrayscale { get; private set; }
		public bool DatabaseLoaded { get; private set; }
		public int TrainingPatternsCount { get; private set; }
		public int TestingPatternsCount { get; private set; }
		public int[] RandomTrainingPattern { get; private set; }
		public DoubleImageData[] TrainingPatterns { get; private set; }
		public DoubleImageData[] TestingPatterns { get; private set; }
		public DoubleImageData[] PreparedTrainingPatterns { get; private set; }
		public ByteImageData[] MNISTTraining { get; private set; }
		public ByteImageData[] MNISTTesting { get; private set; }
		public int MNistWidth { get; private set; }
		public int MNistHeight { get; private set; }
		public int MNistSize { get; private set; }
		public int PatternWidth { get; private set; }
		public int PatternHeight { get; private set; }
		public int PatternSize { get; private set; }
		public double Foreground { get; private set; }
		public double Background { get; private set; }
		public double Divisor { get; private set; }

		public double SeverityFactor = 1.0D;
		public double MaxScaling = 20.0D;      // like 20.0 for 20%
		public double MaxRotation = 15.0D;     // like 20.0 for 20 degrees
		public double ElasticSigma = 8.0D;     // higher numbers are more smooth and less distorted; Simard uses 4.0
		public double ElasticScaling = 0.5D;   // higher numbers amplify the distortions; Simard uses 34 (sic, maybe 0.34 ??)
		
		public ParallelOptions parallelOption { get; private set; }

		private int _maxDegreeOfParallelism = Environment.ProcessorCount;

		public int MaxDegreeOfParallelism
		{
			get
			{
				return _maxDegreeOfParallelism;
			}
			set
			{
				if (value == _maxDegreeOfParallelism)
					return;

			    if ((value == 0) || (value > Environment.ProcessorCount))
					_maxDegreeOfParallelism = -1;
				else
					_maxDegreeOfParallelism = value;

				parallelOption.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
			}
		}

		private int GaussianFieldSize = 21;     // strictly odd number (21)
		private int Mid;
		private double[,] GaussianKernel;
		static int patternSize = 32 * 32;
		private double[] DispH = new double[patternSize];
		private double[] DispV = new double[patternSize];
		private double[] uniformH = new double[patternSize];
		private double[] uniformV = new double[patternSize];
		private double[] mappedVector = new double[patternSize];
		private int iMid;
		private double ElasticScale;
		private double AngleFixed;
		private double ScaleFixed;
		private ThreadSafeRandom RandomGenerator = new ThreadSafeRandom();
		private System.Diagnostics.Stopwatch stopwatch;

		private enum DataType : byte
		{
			typeUnsignedByte = (byte)0x08,
			typeSignedByte = (byte)0x09,
			typeShort = (byte)0x0B,
			typeInteger = (byte)0x0C,
			typeFloat = (byte)0x0D,
			typeDouble = (byte)0x0E,
		};

		protected virtual void OnRaiseDataLoadedEvent(DataProviderEventArgs e)
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			EventHandler<DataProviderEventArgs> handler = null;
			
			lock(this)
			{
				handler = RaiseDataLoadedEvent;
			}
			// Event will be null if there are no subscribers

			if (handler != null)
			{
				foreach (EventHandler<DataProviderEventArgs> _handler in handler.GetInvocationList())
				{
					try
					{

						System.Windows.Application.Current.Dispatcher.Invoke(_handler, new object[] { this, e });

					}
					catch (Exception ex)
					{
						Debug.WriteLine("Error in the handler {0}: {1}", handler.Method.Name, ex.Message);
					}
				}
			}
		}

		protected virtual void OnRaiseDataProgressEvent(DataProviderEventArgs e)
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			EventHandler<DataProviderEventArgs> handler = null;

			lock (this)
			{
				handler = RaiseDataProgressEvent;
			}
			// Event will be null if there are no subscribers

			if (handler != null)
			{
				foreach (EventHandler<DataProviderEventArgs> _handler in handler.GetInvocationList())
				{
					try
					{

						System.Windows.Application.Current.Dispatcher.Invoke(_handler,new object[]{this, e});
					}
					catch (Exception ex)
					{
						Debug.WriteLine("Error in the handler {0}: {1}", handler.Method.Name, ex.Message);
					}
				}
			}
		}

		public DataProvider()
		{
			stopwatch = new Stopwatch();
			parallelOption = new ParallelOptions();
			parallelOption.TaskScheduler = null;
            _maxDegreeOfParallelism = Environment.ProcessorCount;
			parallelOption.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
		}
				
		~DataProvider()
		{
			// In case the client forgets to call
			// Dispose , destructor will be invoked for
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// dispose managed resources
				if (RandomGenerator != null)
					RandomGenerator.Dispose();
			}
			// free native resources
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
        private void LoadHandTestingPatternsFromDir(string path)
        {
            try
            {
                byte[] TestPatterns;
                MNistHeight = 32;
                MNistWidth = 32;
                MNistSize = MNistWidth * MNistHeight;
                int TrainingLabelCount = 9;
                int LabelImageCount = 100;
                TestingPatternsCount = TrainingLabelCount * LabelImageCount;
                TestPatterns = new byte[TestingPatternsCount * MNistSize];
                //Capture cap = new Capture(@"D:\ebooks\hand gestrue recognition\hand data set\mov\0.MOV");
                unsafe
                {

                    for (int ii = 0; ii < TrainingLabelCount; ii++)
                    {
                        string type = ii.ToString("D1");
                        //Image<Bgr, Byte> image = new Image<Bgr, byte>(path + "\\" + type + ".jpg").Resize(32, 32, Emgu.CV.CvEnum.INTER.CV_INTER_AREA); //Read the files as an 8-bit Bgr image  
                        //Image<Gray, Byte> gray = image.Convert<Gray, Byte>(); //Convert it to Grayscale
                        Capture cap = new Capture(path + "\\" + type + ".MOV");
                        for(int i =0; i<200;i++)
                        {
                            cap.QueryGrayFrame();//skip first 200 frames
                        }
                        for (int i = 0; i < LabelImageCount; i++)
                        {
                            Image<Gray, Byte> gray = cap.QueryGrayFrame().Resize(32, 32, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
                            for (int j = 0; j < MNistSize; j++)
                            {
                                TestPatterns[ii * MNistSize * LabelImageCount + i * MNistSize + j] = ((byte*)gray.MIplImage.imageData + j)[0];
                            }
                        }
                        cap.Dispose();
                    }
                }


                MNISTTesting = new ByteImageData[TestingPatternsCount];
                Parallel.For(0, TestingPatternsCount, parallelOption, j =>
                {
                    ByteImageData pattern = new ByteImageData(j / LabelImageCount, new byte[MNistSize]);
                    for (int i = 0; i < MNistSize; i++)
                    {
                        pattern.Image[i] = TestPatterns[(j * MNistSize) + i];
                    }
                    MNISTTesting[j] = pattern;
                });

            }
            catch (Exception)
            {
                throw;
            }
        }
        private void LoadHandTrainingPatternsFromDir(string path)
        {
            try
            {
                byte[] TrainPatterns;
                MNistHeight = 32;
                MNistWidth = 32;
                MNistSize = MNistWidth * MNistHeight;
                int TrainingLabelCount = 10;
                int LabelImageCount = 200;
                TrainingPatternsCount = TrainingLabelCount*LabelImageCount;

                TrainPatterns = new byte[TrainingPatternsCount * MNistSize];
                unsafe
                {

                    for (int ii = 0; ii < TrainingLabelCount; ii++)
                    {
                        string type = ii.ToString("D1");
                        //Image<Bgr, Byte> image = new Image<Bgr, byte>(path + "\\" + type + ".jpg").Resize(32, 32, Emgu.CV.CvEnum.INTER.CV_INTER_AREA); //Read the files as an 8-bit Bgr image  
                        //Image<Gray, Byte> gray = image.Convert<Gray, Byte>(); //Convert it to Grayscale
                        Capture cap = new Capture(path + "\\" + type + ".MOV");
                        for (int i = 0; i < LabelImageCount; i++)
                        {
                            Image<Gray, Byte> gray = cap.QueryGrayFrame().Resize(32, 32, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
                            for (int j = 0; j < MNistSize; j++)
                            {
                                TrainPatterns[ii * MNistSize * LabelImageCount + i * MNistSize + j] = ((byte*)gray.MIplImage.imageData + j)[0];
                            }
                        }
                        cap.Dispose();
                    }
                }
                MNISTTraining = new ByteImageData[TrainingPatternsCount];
                Parallel.For(0, TrainingPatternsCount, parallelOption, j =>
                {
                    int label = j / LabelImageCount;
                    ByteImageData imageData = new ByteImageData(label, new byte[MNistSize]);
                    for (int i = 0; i < MNistSize; i++)
                    {
                        imageData.Image[i] = TrainPatterns[(j * MNistSize) + i];
                    }
                    MNISTTraining[j] = imageData;
                });

            }
            catch (Exception)
            {
                throw;
            }
        }
		private void LoadTrainingPatternsFromFile(string path)
		{
			try
			{
				byte[] TrainPatterns;
				using (FileStream fileStreamTrainPatterns = System.IO.File.Open(path + @"\train-images-idx3-ubyte", FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					byte[] magicNumber = new byte[16];
					fileStreamTrainPatterns.Read(magicNumber, 0, 16);
					//DataType dataType = (DataType)magicNumber[2];
					//int dimensions = (int)magicNumber[3];

					TrainingPatternsCount = magicNumber[7] + (magicNumber[6] * 256) + (magicNumber[5] * 65536) + (magicNumber[4] * 16777216);
					MNistHeight = magicNumber[11] + (magicNumber[10] * 256) + (magicNumber[9] * 65536) + (magicNumber[8] * 16777216);
					MNistWidth = magicNumber[15] + (magicNumber[14] * 256) + (magicNumber[13] * 65536) + (magicNumber[12] * 16777216);
					MNistSize = MNistWidth * MNistHeight;

					TrainPatterns = new byte[TrainingPatternsCount * MNistSize];
					fileStreamTrainPatterns.Seek(16, SeekOrigin.Begin);
					fileStreamTrainPatterns.Read(TrainPatterns, 0, TrainingPatternsCount * MNistSize);
				}

				byte[] TrainLabels;
				using (FileStream fileStreamTrainLabels = System.IO.File.Open(path + @"\train-labels-idx1-ubyte", FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					TrainLabels = new byte[TrainingPatternsCount];
					fileStreamTrainLabels.Seek(8, SeekOrigin.Begin);
					fileStreamTrainLabels.Read(TrainLabels, 0, TrainingPatternsCount);
				}

				MNISTTraining = new ByteImageData[TrainingPatternsCount];
				Parallel.For (0, TrainingPatternsCount , parallelOption , j =>
				{
					ByteImageData imageData = new ByteImageData(TrainLabels[j], new byte[MNistSize]);
					for (int i = 0; i < MNistSize; i++)
					{
						imageData.Image[i] = TrainPatterns[(j * MNistSize) + i];
					}
					MNISTTraining[j] = imageData;
				});
			}
			catch (Exception)
			{
				throw;
			}           
		}

		private void LoadTestingPatternsFromFile(string path)
		{
			try
			{
				byte[] TestPatterns;
				using (FileStream fileStreamTestPatterns = System.IO.File.Open(path + @"\t10k-images-idx3-ubyte", FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					byte[] magicNumber = new byte[16];
					fileStreamTestPatterns.Read(magicNumber, 0, 16);
					//DataType dataType = (DataType)magicNumber[2];
					//int dimensions = (int)magicNumber[3];

					TestingPatternsCount = magicNumber[7] + (magicNumber[6] * 256) + (magicNumber[5] * 65536) + (magicNumber[4] * 16777216);
					MNistHeight = magicNumber[11] + (magicNumber[10] * 256) + (magicNumber[9] * 65536) + (magicNumber[8] * 16777216);
					MNistWidth = magicNumber[15] + (magicNumber[14] * 256) + (magicNumber[13] * 65536) + (magicNumber[12] * 16777216);
					MNistSize = MNistWidth * MNistHeight;

					TestPatterns = new byte[TestingPatternsCount * MNistSize];
					fileStreamTestPatterns.Seek(16, SeekOrigin.Begin);
					fileStreamTestPatterns.Read(TestPatterns, 0, TestingPatternsCount * MNistSize);
				}

				byte[] TestLabels;
				using (FileStream fileStreamTestLabels = System.IO.File.Open(path + @"\t10k-labels-idx1-ubyte", FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					TestLabels = new byte[TestingPatternsCount];
					fileStreamTestLabels.Seek(8, SeekOrigin.Begin);
					fileStreamTestLabels.Read(TestLabels, 0, TestingPatternsCount);
				}

				MNISTTesting = new ByteImageData[TestingPatternsCount];
				Parallel.For(0, TestingPatternsCount, parallelOption, j =>
				{
					ByteImageData pattern = new ByteImageData(TestLabels[j], new byte[MNistSize]);
					for (int i = 0; i < MNistSize; i++)
					{
						pattern.Image[i] = TestPatterns[(j* MNistSize ) + i];
					}
					MNISTTesting[j] = pattern;
				});

			}
			catch (Exception)
			{
				throw;
			}
		}

		public void LoadDataFromFile(string path, int width, int height)
		{
			stopwatch.Start();
			
			Task[] tasks = new Task[2];
			//tasks[0] = Task.Factory.StartNew(() => LoadTrainingPatternsFromFile(path));
			//tasks[1] = Task.Factory.StartNew(() => LoadTestingPatternsFromFile(path));
            //path = @"D:\ebooks\hand gestrue recognition\hand data set\Set1";
            path = @"D:\ebooks\hand gestrue recognition\hand data set\mov";
            tasks[0] = Task.Factory.StartNew(() => LoadHandTrainingPatternsFromDir(path));
            tasks[1] = Task.Factory.StartNew(() => LoadHandTestingPatternsFromDir(path));

			try
			{
				Task.WaitAll(tasks);
			}
			catch (AggregateException ex)
			{
				InformationDialog.Show(null, ex.Message, ex.ToString());
			}
			finally
			{
				tasks[0].Dispose();
				tasks[1].Dispose();
			}
			OnRaiseDataProgressEvent(new DataProviderEventArgs(25, String.Format("MNIST Dataset loaded"), stopwatch.Elapsed));

			GenerateTrainingData(false, width, height);
			OnRaiseDataProgressEvent(new DataProviderEventArgs(75, String.Format("Training Patterns created"), stopwatch.Elapsed));
			Thread.Sleep(350);

			CalculateGaussianKernel();
			OnRaiseDataProgressEvent(new DataProviderEventArgs(100, String.Format("Gaussian Kernel initialized"), stopwatch.Elapsed));
			Thread.Sleep(350);

			GC.Collect();
			stopwatch.Reset();
			OnRaiseDataLoadedEvent(new DataProviderEventArgs(0, String.Format("Data loaded at {0}", DateTime.Now.ToString()), stopwatch.Elapsed));
			Thread.Sleep(350);
		}

		public void ScrambleInputPatterns()
		{
			int l;
			int k;
			for (int i = 0; i < TrainingPatternsCount; i++)
			{
				l = RandomGenerator.Next(0, TrainingPatternsCount);
				k = RandomTrainingPattern[i];
				RandomTrainingPattern[i] = RandomTrainingPattern[l];
				RandomTrainingPattern[l] = k;
			}
		}

		public void GenerateTrainingData(bool flipGrayscale, int width, int height)
		{
			FlipGrayscale = flipGrayscale;
			PatternWidth = width;
			PatternHeight = height;
			PatternSize = width * height;
			patternSize = PatternSize;

			if (FlipGrayscale)
			{
				Background = 1;
				Foreground = -1;
			}
			else
			{
				Background = -1;
				Foreground = 1;
			}

			int xOffset = (PatternWidth - MNistWidth) / 2;
			int yOffset = (PatternHeight - MNistHeight) / 2;
			Divisor = 255D / (Math.Abs(Background) + Math.Abs(Foreground));
			
			TrainingPatterns = new DoubleImageData[TrainingPatternsCount];
			RandomTrainingPattern = new int[TrainingPatternsCount];

			Parallel.For(0, TrainingPatternsCount, parallelOption, i =>
			{
				DoubleImageData pattern = new DoubleImageData(i, new double[PatternSize]);
				for (int j = 0; j < PatternSize; j++)
				{
					pattern.Image[j] = Background;
				}
				TrainingPatterns[i] = pattern;

				RandomTrainingPattern[i] = i;
			});

			Parallel.For(0, TrainingPatternsCount, parallelOption, i =>
			{
				byte temp;
				DoubleImageData trainingPattern = TrainingPatterns[i];
				byte[] image = MNISTTraining[i].Image;
				trainingPattern.Label = MNISTTraining[i].Label;
				
				for (int y = 0; y < MNistHeight; y++)
				{
					for (int x = 0; x < MNistWidth; x++)
					{
						temp = image[x + (y * MNistWidth)];
						{
							trainingPattern.Image[(x + xOffset) + ((y + yOffset) * PatternWidth)] = ((((double)temp) / Divisor) - 1D);
						}
					}
				}

				TrainingPatterns[i] = trainingPattern;
			});

			MNISTTraining = null;
			TestingPatterns = new DoubleImageData[TestingPatternsCount];
			Parallel.For(0, TestingPatternsCount, parallelOption, i =>
			{
				DoubleImageData pattern = new DoubleImageData(i, new double[PatternSize]);
				for (int j = 0; j < PatternSize; j++)
				{
					pattern.Image[j] = Background;
				}
				TestingPatterns[i] = pattern;
			});

			Parallel.For(0, TestingPatternsCount, parallelOption, i =>
			{
				byte temp;
				DoubleImageData testingPattern = TestingPatterns[i];
				byte[] image = MNISTTesting[i].Image;
				testingPattern.Label = MNISTTesting[i].Label;
				
				for (int y = 0; y < MNistHeight; y++)
				{
					for (int x = 0; x < MNistWidth; x++)
					{
						temp = image[x + (y * MNistWidth)];
						{
							testingPattern.Image[(x + xOffset) + ((y + yOffset) * PatternWidth)] = ((((double)temp / Divisor)) - 1D);
						}
					}
				}

				TestingPatterns[i] = testingPattern;
			});

			MNISTTesting = null;
		}

		public void CalculateGaussianKernel()
		{
			if (GaussianKernel == null)
				GaussianKernel = new double[GaussianFieldSize, GaussianFieldSize];

			Mid = GaussianFieldSize / 2;
			iMid = MNistHeight / 2;
			ElasticScale = SeverityFactor * ElasticScaling;
			AngleFixed = SeverityFactor * MaxRotation * Math.PI / 180.0D;
			ScaleFixed = SeverityFactor * MaxScaling / 100.0D;

			double twoSigmaSquared = 1D / (2D * ElasticSigma * ElasticSigma);
			double twoPiSigma = 1D / (2D * Math.PI * ElasticSigma * ElasticSigma);
			//double twoPiSigma = 1D / ElasticSigma * Math.Sqrt(2D * Math.PI);

			Parallel.For(0, GaussianFieldSize, parallelOption, row =>
			{
				for (int col = 0; col < GaussianFieldSize; col++)
				{
					GaussianKernel[row, col] = twoPiSigma * (Math.Exp(-((((row - Mid) * (row - Mid)) + ((col - Mid) * (col - Mid))) * twoSigmaSquared)));
				}
			});
		}

		public void PrepareTrainingData(TrainingRate learningParameters, int epoch, ref int index, ref bool stop)
		{
			if (learningParameters.Distorted)
			{
				if (((epoch-1) % learningParameters.SameDistortionsForNEpochs) == 0)
				{
					bool parameterChanged = false;

					if (SeverityFactor != learningParameters.SeverityFactor)
					{
						SeverityFactor = learningParameters.SeverityFactor;
						parameterChanged = true;
					}

					if (MaxScaling != learningParameters.MaxScaling)
					{
						MaxScaling = learningParameters.MaxScaling;
						parameterChanged = true;
					}

					if (MaxRotation != learningParameters.MaxRotation)
					{
						MaxRotation = learningParameters.MaxRotation;
						parameterChanged = true;
					}

					if (ElasticSigma != learningParameters.ElasticSigma)
					{
						ElasticSigma = learningParameters.ElasticSigma;
						parameterChanged = true;
					}

					if (ElasticScaling != learningParameters.ElasticScaling)
					{
						ElasticScaling = learningParameters.ElasticScaling;
						parameterChanged = true;
					}
					
					if (parameterChanged)
						CalculateGaussianKernel();

					PreparedTrainingPatterns = new DoubleImageData[TrainingPatternsCount];
					for (index = 0; index < TrainingPatternsCount; index++)
					{
						if (stop)
							break;

						PreparedTrainingPatterns[index] =  new DoubleImageData(TrainingPatterns[index].Label, GetDistortedPattern(index, true));
					}
				}
			}
			else
			{
				PreparedTrainingPatterns = TrainingPatterns;
			}
		}
		
		public double[] GetDistortedPattern(int index, bool trainingSet)
		{
			Parallel.For(0, PatternSize, parallelOption, i =>
			{
				uniformH[i] = ((RandomGenerator.NextDouble() * 2D) - 1D);
				uniformV[i] = ((RandomGenerator.NextDouble() * 2D) - 1D);
			});

			Parallel.For(0, PatternWidth, parallelOption, col =>
			{
				for (int row = 0; row < PatternHeight; row++)
				{
					double fConvolvedH = 0.0D;
					double fConvolvedV = 0.0D;

					for (int xxx = 0; xxx < GaussianFieldSize; xxx++)
					{
						for (int yyy = 0; yyy < GaussianFieldSize; yyy++)
						{
							int xxxDisp = col - Mid + xxx;
							int yyyDisp = row - Mid + yyy;

							double fSampleH;
							double fSampleV;

							if (xxxDisp < 0 || xxxDisp >= PatternWidth || yyyDisp < 0 || yyyDisp >= PatternHeight)
							{
								fSampleH = 0.0D;
								fSampleV = 0.0D;
							}
							else
							{
								fSampleH = uniformH[xxxDisp + (yyyDisp * PatternWidth)];
								fSampleV = uniformV[xxxDisp + (yyyDisp * PatternWidth)];
							}

							fConvolvedH += fSampleH * GaussianKernel[yyy, xxx];
							fConvolvedV += fSampleV * GaussianKernel[yyy, xxx];
						}
					}

					DispH[col + (row * PatternWidth)] = ElasticScale * fConvolvedH;
					DispV[col + (row * PatternWidth)] = ElasticScale * fConvolvedV;
				}
			});

			// next, the scaling of the image by a random scale factor
			// Horizontal and vertical directions are scaled independently
			double dSFHoriz = ScaleFixed * ((RandomGenerator.NextDouble() * 2D) - 1D);  // MaxScaling is a percentage
			double dSFVert = ScaleFixed * ((RandomGenerator.NextDouble() * 2D) - 1D);   // MaxScaling is a percentage

			Parallel.For(0, PatternHeight, parallelOption, row =>
			{
				for (int col = 0; col < PatternWidth; col++)
				{
					DispH[col + (row * PatternWidth)] += dSFHoriz * (col - iMid);
					DispV[col + (row * PatternWidth)] -= dSFVert * (iMid - row);  // negative because of top-down bitmap
				}
			});

			// finally, apply a rotation
			double angle = AngleFixed * ((RandomGenerator.NextDouble() * 2D) - 1D);
			double cosAngle = Math.Cos(angle);
			double sinAngle = Math.Sin(angle);

			Parallel.For(0, PatternHeight, parallelOption, row =>
			{
				for (int col = 0; col < PatternWidth; ++col)
				{
					DispH[col + (row * PatternWidth)] += (col - iMid) * (cosAngle - 1) - (iMid - row) * sinAngle;
					DispV[col + (row * PatternWidth)] -= (iMid - row) * (cosAngle - 1) + (col - iMid) * sinAngle;  // negative because of top-down bitmap
				}
			});

			Parallel.For(0, PatternWidth, parallelOption, col =>
			{
				for (int row = 0; row < PatternHeight; row++)
				{
					double sourceRow = (double)row - DispV[col + (row * PatternWidth)];
					double sourceCol = (double)col - DispH[col + (row * PatternWidth)];

					double fracRow = sourceRow - (int)sourceRow;
					double fracCol = sourceCol - (int)sourceCol;

					double w1 = (1.0D - fracRow) * (1.0D - fracCol);
					double w2 = (1.0D - fracRow) * fracCol;
					double w3 = fracRow * (1.0D - fracCol);
					double w4 = fracRow * fracCol;

					bool bSkipOutOfBounds = false;

					if ((sourceRow + 1.0D) >= PatternHeight) bSkipOutOfBounds = true;
					if (sourceRow < 0D) bSkipOutOfBounds = true;

					if ((sourceCol + 1.0D) >= PatternWidth) bSkipOutOfBounds = true;
					if (sourceCol < 0D) bSkipOutOfBounds = true;

					double sourceValue;
					
					if (bSkipOutOfBounds == false)
					{
						// the supporting pixels for the "phantom" source pixel are all within the 
						// bounds of the character grid.
						// Manufacture its value by bi-linear interpolation of surrounding pixels

						int sRow = (int)sourceRow;
						int sCol = (int)sourceCol;

						int sRowp1 = sRow + 1;
						int sColp1 = sCol + 1;

						while (sRowp1 >= PatternHeight) sRowp1 -= PatternHeight;
						while (sRowp1 < 0) sRowp1 += PatternHeight;

						while (sColp1 >= PatternWidth) sColp1 -= PatternWidth;
						while (sColp1 < 0) sColp1 += PatternWidth;

						// perform bi-linear interpolation
						if (trainingSet)
						{
							sourceValue = (w1 * TrainingPatterns[index].Image[(sRow * PatternWidth) + sCol]) + (w2 * TrainingPatterns[index].Image[(sRow * PatternWidth) + sColp1]) + (w3 * TrainingPatterns[index].Image[(sRowp1 * PatternWidth) + sCol]) + (w4 * TrainingPatterns[index].Image[(sRowp1 * PatternWidth) + sColp1]);
						}
						else
						{
							sourceValue = (w1 * TestingPatterns[index].Image[(sRow * PatternWidth) + sCol]) + (w2 * TestingPatterns[index].Image[(sRow * PatternWidth) + sColp1]) + (w3 * TestingPatterns[index].Image[(sRowp1 * PatternWidth) + sCol]) + (w4 * TestingPatterns[index].Image[(sRowp1 * PatternWidth) + sColp1]);
						}
					}
					else
					{
						// At least one supporting pixel for the "phantom" pixel is outside the
						// bounds of the character grid. Set its value to "background"

						sourceValue = Background;  // "background" color in the -1 -> +1 range of inputVector
					}

					mappedVector[col + (row * PatternWidth)] = 0.5D * (1.0D - sourceValue);  // conversion to 0->1 range we are using for mappedVector
				}
			});

			double[] Pattern = new double[PatternSize];
			for (int i = 0; i < PatternSize; i++)
			{
				Pattern[i] = 1.0D - (2.0D * mappedVector[i]);
			}

			return (Pattern);
		}
	}
}