using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CNNWB.Common;


namespace CNNWB.Model
{
	public enum LossFunctions
	{
		CrossEntropy = 0,
		MeanSquareError = 1,
		NegativeLogLikelihood = 2,
	}

	public enum LayerTypes
	{
		Convolutional = 0,
		ConvolutionalSubsampling = 1,
		FullyConnected = 2,
		Input = 3,
		//Normalization = 4,
		RBF = 5,
		//Rectification = 6,
		Subsampling = 7,
	}

	public enum KernelTypes
	{
		AveragePooling = 0,
		Gaussian = 1,
		Linear = 2,
		Logistics = 3,
		MaxPooling = 4,
		MedianPooling = 5,
		None = 6,
		Sigmoid = 7,
	}

	[Serializable()]
	public class Mappings
	{
		public NeuralNetworks Network;
		public int LayerIndex;
		public List<bool> IsMapped;

		public Mappings(NeuralNetworks network, int layerIndex, List<bool> isMapped) 
		{
			Network = network;
			LayerIndex = layerIndex;
			IsMapped = isMapped;
		}
	}

	[Serializable()]
	public struct Connection
	{
		public int ToNeuronIndex;
		public int ToWeightIndex;

		public Connection(int toNeuronIndex, int toWeightIndex): this()
		{
			ToNeuronIndex = toNeuronIndex;
			ToWeightIndex = toWeightIndex;
		}
	}

	[Serializable()]
	public struct Weight
	{
		public double Value;
		public double DiagonalHessian;
	}

	[Serializable()]
	public class Neuron
	{
		public double Output;
		public Connection[] Connections;

		public Neuron()
		{
			Output = 0D;
			Connections = new Connection[0];
		}

		public void AddConnection(int neuronIndex, int weightIndex)
		{
			Array.Resize(ref Connections, Connections.Length + 1);
			Connections[Connections.Length - 1] = new Connection(neuronIndex, weightIndex);
		}

		public void AddBias(int weightIndex)
		{
			Array.Resize(ref Connections, Connections.Length + 1);
			Connections[Connections.Length - 1] = new Connection(int.MaxValue, weightIndex);
		}
	}

	[Serializable()]
	public class TrainingRate
	{
		public double Rate { get; set; }
		public int Epochs { get; set; }
		public double MinimumRate { get; set; }
		public double DecayFactor { get; set; }
		public int DecayAfterEpochs { get; set; }
		public double WeightSaveTreshold { get; set; }
		public bool Distorted { get; set; }
		public int SameDistortionsForNEpochs { get; set; }
		public double SeverityFactor { get; set; }
		public double MaxScaling { get; set; }
		public double MaxRotation { get; set; }
		public double ElasticSigma { get; set; }
		public double ElasticScaling { get; set; }

		public TrainingRate()
		{
			Rate = 0.0005D;
			//Rate = 0.001D; // value for Simard based nets
			Epochs = 28;
			DecayFactor = 0.65;
			MinimumRate = 0.000001D;
			//DecayFactor = 0.794183335D; // value for Simard based nets
			//MinimumRate = 0.00005D; // value for Simard based nets
			DecayAfterEpochs = 2;
			WeightSaveTreshold = 0.80D;
			Distorted = true;
			SameDistortionsForNEpochs = 1;
			SeverityFactor = 1D;
			MaxScaling = 15D;
			MaxRotation = 15D;
			ElasticSigma = 8.0D;
			ElasticScaling = 0.5D;
		}

		public TrainingRate(bool useDistortions)
		{
			Rate = 0.0005D;
			//Rate = 0.001D; // value for Simard based nets
			Epochs = 28;
			DecayFactor = 0.65;
			MinimumRate = 0.000001D;
			//DecayFactor = 0.794183335D; // value for Simard based nets
			//MinimumRate = 0.00005D; // value for Simard based nets
			DecayAfterEpochs = 2;
			WeightSaveTreshold = 0.80D;
			Distorted = useDistortions;
			SameDistortionsForNEpochs = 1;
			SeverityFactor = 1D;
			MaxScaling = 15D;
			MaxRotation = 15D;
			ElasticSigma = 8D;
			ElasticScaling = 0.5D;
		}

		public TrainingRate(double rate, int epochs, double minRate, double decayFactor, int decayAfterEpochs, double weightSaveErrorTreshold, bool distorted, int sameDistortionsForEpochs, double severityFactor, double maxScaling, double maxRotation, double elasticSigma, double elasticScaling)
		{
			Rate = rate;
			Epochs = epochs;
			MinimumRate = minRate;
			DecayFactor = decayFactor;
			DecayAfterEpochs = decayAfterEpochs;
			WeightSaveTreshold = weightSaveErrorTreshold;
			Distorted = distorted;
			SameDistortionsForNEpochs = sameDistortionsForEpochs;
			SeverityFactor = severityFactor;
			MaxScaling = maxScaling;
			MaxRotation = maxRotation;
			ElasticSigma = elasticSigma;
			ElasticScaling = elasticScaling;
		}
	}

	[Serializable()]
	public class NeuralNetworks: IDisposable 
	{
		public Guid Id { get; private set; }
		public string Name { get; private set; }

		private string _description = String.Empty;
		public string Description
		{
			get
			{
				_description = String.Empty;
				_description += this.Name +"\r\n\r\n";
				foreach (Layers layer in this.Layers)
				{
					_description += layer.Name + "\r\n";
				}

				return _description;
			}
		}

		public double TrainToValue { get; set; }
		public LossFunctions LossFunction { get; set; }
		public double dMicron { get; private set; }
		public DateTime CreatedOn { get; private set; }
		public TrainingRate TrainingRate { get; set; }
		public List<Layers> Layers { get; private set; }
		public List<TrainingRate> TrainingRates { get; private set; }
		public List<List<Byte>> RbfWeights {get; private set;}
		public ThreadSafeRandom RandomGenerator { get; private set; }
		public ParallelOptions ParallelOption {get; private set;}

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

				_maxDegreeOfParallelism = value;
				ParallelOption.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
			}
		}

		
		public NeuralNetworks(string name = "Neural Network", double trainTo = 0.8D, LossFunctions lossFunction = LossFunctions.MeanSquareError, double dmicron = 0.02D)
		{

			Id = Guid.NewGuid();
			Name = name.Trim();
			TrainToValue = trainTo;
			LossFunction = lossFunction;
			dMicron = dmicron;

			CreatedOn = DateTime.Now;
			Layers = new List<Layers>();
			RbfWeights = GetRbfWeightPatterns();
			RandomGenerator = new ThreadSafeRandom();
			ParallelOption = new ParallelOptions();
			ParallelOption.TaskScheduler = null;
			ParallelOption.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
		}

		~NeuralNetworks()
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
				Layers.Clear();
				TrainingRates.Clear();
				RbfWeights.Clear();
				RandomGenerator.Dispose();
				
				Layers = null;
				TrainingRates = null;
				RbfWeights = null;
				RandomGenerator = null;
			}
			// Free unmanaged objects
		}

		public void Dispose()
		{
			Dispose(true);
			// Ensure that the destructor is not called
			GC.SuppressFinalize(this);
		}

		public void Save(string fileName, bool includeWeights = false)
		{
			if (fileName.Contains("-gz"))
			{
				using (NeuralNetworkDataSet ds = new NeuralNetworkDataSet())
				{
					NeuralNetworkDataSet.LossFunctionsRow lossFunctionRow;
					NeuralNetworkDataSet.LayerTypesRow layerTypeRow;
					NeuralNetworkDataSet.KernelTypesRow kernelTypeRow;

					ds.LossFunctions.BeginLoadData();
					foreach (string lossFunctionName in Enum.GetNames(typeof(LossFunctions)))
					{
						ds.LossFunctions.AddLossFunctionsRow(lossFunctionName);
					}
					ds.LossFunctions.EndLoadData();

					ds.LayerTypes.BeginLoadData();
					foreach (string layerTypeName in Enum.GetNames(typeof(LayerTypes)))
					{
						ds.LayerTypes.AddLayerTypesRow(layerTypeName);
					}
					ds.LayerTypes.EndLoadData();

					ds.KernelTypes.BeginLoadData();
					foreach (string kernelTypeName in Enum.GetNames(typeof(KernelTypes)))
					{
						ds.KernelTypes.AddKernelTypesRow(kernelTypeName);
					}
					ds.KernelTypes.EndLoadData();

					lossFunctionRow = ds.LossFunctions.FindByLossFunction((int)this.LossFunction);

					ds.NeuralNetworks.BeginLoadData();
					NeuralNetworkDataSet.NeuralNetworksRow networkRow = ds.NeuralNetworks.AddNeuralNetworksRow(this.Id, this.Name, this.CreatedOn, this.TrainToValue, lossFunctionRow, this.dMicron);
					ds.NeuralNetworks.EndLoadData();


					ds.Layers.BeginLoadData();
					foreach (Layers layer in Layers)
					{
						layerTypeRow = ds.LayerTypes.FindByLayerType((int)layer.LayerType);
						kernelTypeRow = ds.KernelTypes.FindByKernelType((int)layer.KernelType);

						ds.Layers.AddLayersRow(networkRow, layer.LayerIndex, layerTypeRow, kernelTypeRow, layer.NeuronCount, layer.UseMapInfo, layer.MapCount, layer.MapWidth, layer.MapHeight, layer.IsFullyMapped, layer.ReceptiveFieldWidth, layer.ReceptiveFieldHeight, layer.LockedWeights);

						if (layer.Mappings != null)
						{
							ds.Mappings.BeginLoadData();
							int previousMapIndex = 0;
							int currentMapIndex = 0;
							foreach (bool mapped in layer.Mappings.IsMapped)
							{
								ds.Mappings.AddMappingsRow(this.Id, layer.LayerIndex, previousMapIndex, currentMapIndex, mapped);

								currentMapIndex++;
								if (currentMapIndex >= layer.MapCount)
								{
									currentMapIndex = 0;
									previousMapIndex++;
								}
							}
							ds.Mappings.EndLoadData();
						}

						if (includeWeights)
						{
							ds.Weights.BeginLoadData();
							int weightIndex = 0;
							foreach (Weight weight in layer.Weights)
							{
								ds.Weights.AddWeightsRow(this.Id, layer.LayerIndex, weightIndex, weight.Value, weight.DiagonalHessian);
								weightIndex++;
							}
							ds.Weights.EndLoadData();
						}

					}
					ds.Layers.EndLoadData();

					using (FileStream outFile = File.Create(fileName))
					{
						using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
						{
							ds.WriteXml(Compress, System.Data.XmlWriteMode.WriteSchema);
						}
					}
				}
			}
		}

		public static NeuralNetworks Load(string fileName, bool includeWeights = false)
		{
			NeuralNetworks network = null;

			if (fileName.Contains("-gz"))
			{
				using (NeuralNetworkDataSet ds = new NeuralNetworkDataSet())
				{
					using (FileStream inFile = File.OpenRead(fileName))
					{
						using (GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress))
						{
							ds.ReadXml(Decompress, XmlReadMode.ReadSchema);
						}
					}

					if (ds.NeuralNetworks.Rows.Count == 1)
					{
						network = new NeuralNetworks();

						NeuralNetworkDataSet.NeuralNetworksRow networkRow = ds.NeuralNetworks.First();
						network.Id = networkRow.NetworkId;
						network.Name = networkRow.Name;
						network.TrainToValue = networkRow.TrainTo;
						network.LossFunction = (LossFunctions)networkRow.LossFunction;
						network.CreatedOn = networkRow.CreatedOn;
						network.dMicron = networkRow.DMicron;
						

						Layers layer;
						Layers previousLayer = null;
						foreach (NeuralNetworkDataSet.LayersRow layerRow in networkRow.GetLayersRows())
						{
							List<bool> isMapped = new List<bool>();
							foreach (NeuralNetworkDataSet.MappingsRow mappingRow in layerRow.GetMappingsRows())
							{
								isMapped.Add(mappingRow.IsMapped);
							}

							Mappings mappings = null;
							if (isMapped.Count > 0)
								mappings = new Mappings(network, layerRow.LayerIndex, isMapped);

							layer = new Layers(network, layerRow.LayerIndex, (LayerTypes)layerRow.LayerType, (KernelTypes)layerRow.KernelType, layerRow.NeuronCount, layerRow.UseMapInfo, layerRow.MapCount, layerRow.MapWidth, layerRow.MapHeight, layerRow.IsFullyMapped, layerRow.ReceptiveFieldWidth, layerRow.ReceptiveFieldHeight, previousLayer, mappings, layerRow.LockedWeights);
							if ((includeWeights) && (layerRow.GetWeightsRows().Count() > 0))
							{
								int i = 0;
								foreach (NeuralNetworkDataSet.WeightsRow weightRow in layerRow.GetWeightsRows())
								{
									layer.Weights[i].Value = weightRow.Value;
									layer.Weights[i].DiagonalHessian = weightRow.DiagonalHessian;
									i++;
								}
							}
							network.Layers.Add(layer);
							previousLayer = layer;
						}

						if (!includeWeights)
							network.InitWeights(true);
					}
					else
					{
						InformationDialog.Show(null, "Invalid data format.", "Select an different file", "Information");
					}
				}
			}

			return network;
		}

		public void SaveWeights(string fileName)
		{
			if (fileName.Contains("-gz"))
			{
				// The DataSet name becomes the root XML element
				using (DataSet MyDataSet = new DataSet("Weights"))
				{
					// This can be confusing, the 'DataTable' will actually
					// become Elements (Rows) in the XML file.
					using (DataTable MyDataTable = new DataTable("Weight"))
					{
						MyDataTable.Columns.Add(new DataColumn("LayerIndex", typeof(System.Int32), null, MappingType.Attribute));
						MyDataTable.Columns.Add(new DataColumn("WeightIndex", typeof(System.Int32), null, MappingType.Attribute));
						MyDataTable.Columns.Add(new DataColumn("Value", typeof(Double), null, MappingType.Attribute));
						MyDataTable.Columns.Add(new DataColumn("DiagonalHessian", typeof(Double), null, MappingType.Attribute));
						MyDataSet.Tables.Add(MyDataTable);

						DataRow tempRow;
						foreach (Layers layer in this.Layers)
						{
							int weightIndex = 0;
							foreach (Weight weight in layer.Weights)
							{
								tempRow = MyDataTable.NewRow();
								tempRow["LayerIndex"] = layer.LayerIndex;
								tempRow["WeightIndex"] = weightIndex++;
								tempRow["Value"] = weight.Value;
								tempRow["DiagonalHessian"] = weight.DiagonalHessian;
								MyDataTable.Rows.Add(tempRow);
							}
						}
					}

					using (FileStream outFile = File.Create(fileName))
					{
						using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
						{
							MyDataSet.WriteXml(Compress, System.Data.XmlWriteMode.WriteSchema);
						}
					}
				}
			}
		}

		public void LoadWeights(string fileName)
		{
			// unzip file
			if (fileName.Contains("-gz"))
			{
				using (DataSet MyDataSet = new DataSet("Weights"))
				{
					using (FileStream inFile = File.OpenRead (fileName))
					{
						using (GZipStream Compress = new GZipStream(inFile, CompressionMode.Decompress))
						{
							MyDataSet.ReadXml(Compress, XmlReadMode.ReadSchema);
						}
					}

					foreach (DataRow row in MyDataSet.Tables["Weight"].Rows)
					{
						this.Layers[(Int32)row["LayerIndex"]].Weights[(Int32)row["WeightIndex"]].Value = (Double)row["Value"];
						this.Layers[(Int32)row["LayerIndex"]].Weights[(Int32)row["WeightIndex"]].DiagonalHessian = (Double)row["DiagonalHessian"];
					}
				}
			}
		}
		
		public void SetStandardLearingRates(bool distorted = true)
		{
			if (TrainingRates != null)
				TrainingRates.Clear();
			else
				TrainingRates = new List<TrainingRate>();

			TrainingRates.Add(new TrainingRate(0.0005D, 2, 0.000001D, 1, 1, 0.82D, distorted, 1, 1, 15, 15, 8, 0.5));
			TrainingRates.Add(new TrainingRate(0.0002D, 3, 0.000001D, 1, 1, 0.82D, distorted, 1, 1, 15, 15, 8, 0.5));
			TrainingRates.Add(new TrainingRate(0.0001D, 3, 0.000001D, 1, 1, 0.82D, distorted, 1, 1, 15, 15, 8, 0.5));
			TrainingRates.Add(new TrainingRate(0.00005D, 4, 0.000001D, 1, 1, 0.82D, distorted, 1, 1, 15, 15, 8, 0.5));
			TrainingRates.Add(new TrainingRate(0.00001D, 8, 0.000001D, 1, 1, 0.82D, distorted, 1, 1, 15, 15, 8, 0.5));
			TrainingRates.Add(new TrainingRate(0.00001D, 6, 0.000001D, 1, 1, 0.82D, false, 1, 1, 15, 15, 8, 0.5));
		}

		public void AddGlobalTrainingRate(TrainingRate rate, Boolean clear = true)
		{
			 if (TrainingRates == null)
				 TrainingRates = new List<TrainingRate>();

			if ((TrainingRates != null) && (clear))
				TrainingRates.Clear();

			if (rate.DecayFactor == 1D)
			{
				TrainingRates.Add(rate);

				int numEpochs = 0;
				foreach (TrainingRate aRate in TrainingRates)
				{
					numEpochs += aRate.Epochs;
				}
			}
			else
			{
				// Decaying Training Rate
				if (rate.Epochs < rate.DecayAfterEpochs)
					rate.DecayAfterEpochs = rate.Epochs;

				int totIteration = rate.Epochs / rate.DecayAfterEpochs;
				double newRating = rate.Rate;
				
				for (int i = 0; i < totIteration; i++)
				{
					TrainingRates.Add(new TrainingRate(newRating, rate.DecayAfterEpochs, rate.MinimumRate, rate.DecayFactor, rate.DecayAfterEpochs, rate.WeightSaveTreshold, rate.Distorted, rate.SameDistortionsForNEpochs, rate.SeverityFactor, rate.MaxScaling, rate.MaxRotation, rate.ElasticSigma, rate.ElasticScaling));
					if (newRating * rate.DecayFactor > rate.MinimumRate)
						newRating *= rate.DecayFactor;
					else
						newRating = rate.MinimumRate;
				}

				if ((totIteration * rate.DecayAfterEpochs) < rate.Epochs)
				{
					TrainingRates.Add(new TrainingRate(newRating, rate.Epochs - (totIteration * rate.DecayAfterEpochs), rate.MinimumRate, rate.DecayFactor, rate.DecayAfterEpochs, rate.WeightSaveTreshold, rate.Distorted, rate.SameDistortionsForNEpochs, rate.SeverityFactor, rate.MaxScaling, rate.MaxRotation, rate.ElasticSigma, rate.ElasticScaling));
				}
			}
		}

		public void InitWeights(bool useNeuronCount = true, double weightScope = 2D, double weightFactor = 1D)
		{
			foreach (Layers layer in Layers)
			{
				if (layer.LayerType == LayerTypes.Input) 
					continue;

				layer.SetInitalWeights(useNeuronCount, weightScope, weightFactor);
			}
		}

		public BitmapSource bitmapToSource(System.Drawing.Image image)
		{
			MemoryStream stream = new MemoryStream();
			image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
			PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

			BitmapSource destination = decoder.Frames[0];
			destination.Freeze();
			
			return destination;
		} 

		public List<List<byte>> GetRbfWeightPatterns()
		{
			ResourceManager rm = new ResourceManager("CNNWB.Properties.Resources", Assembly.GetExecutingAssembly());
			CultureInfo ci = Thread.CurrentThread.CurrentCulture;

			List<List<byte>> rbfImages = new List<List<byte>>(10);
			int width = 7;
			int height = 12;


			for (int i = 0; i < 10; ++i)
			{
				System.Drawing.Image image = rm.GetObject("Image" + i.ToString(), ci) as System.Drawing.Image;
				BitmapSource bitmapSource = bitmapToSource(image);
				bitmapSource.Freeze();
				
				int rawStride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
				byte[] rawImage = new byte[rawStride * height];
				bitmapSource.CopyPixels(rawImage, rawStride, 0);

				List<byte> inputPattern = new List<byte>(rawStride * 12);
				for (int j = 0; j < (rawStride * height); ++j)
				{
					inputPattern.Add(rawImage[j]);
				}

				rbfImages.Add(inputPattern);
			}

			return (rbfImages);
		}

		public void Calculate()
		{
			Layers currentLayer = Layers.First();
			while (currentLayer.NextLayer != null)
			{
				currentLayer = currentLayer.NextLayer;
				currentLayer.Calculate();
			}
		}
				
		public void Backpropagate(int correctAnswer)
		{
			Layers currentLayer = Layers.Last();
			double[] D1ErrX = new double[currentLayer.NeuronCount];
			
			if (currentLayer.LayerType == LayerTypes.RBF)
			{
				switch (currentLayer.Network.LossFunction)
				{
					case LossFunctions.MeanSquareError:
						if (currentLayer.LockedWeights)
						{
							for (int i = 0; i < currentLayer.NeuronCount; i++)
							{
								D1ErrX[i] = currentLayer.Neurons[i].Output + TrainToValue;
							}
							D1ErrX[correctAnswer] = currentLayer.Neurons[correctAnswer].Output - TrainToValue;
							D1ErrX = currentLayer.Backpropagate(D1ErrX);
							currentLayer = currentLayer.PreviousLayer;
							Layers lastLayer = Layers.Last();
							int c = 0;
							int weightIndex = 0;
							for (int j = 0; j < currentLayer.NeuronCount; j++)
							{
								weightIndex = lastLayer.Neurons[correctAnswer].Connections[c++].ToWeightIndex;
								D1ErrX[j] = currentLayer.Neurons[j].Output - lastLayer.Weights[weightIndex].Value;
							}
						}
						break;

					case LossFunctions.NegativeLogLikelihood:
						break;
				}
			}
			else
			{
				switch (currentLayer.Network.LossFunction)
				{
					case LossFunctions.CrossEntropy:
						// Not implemented yet
						break;

					case LossFunctions.MeanSquareError:
						for (int i = 0; i < currentLayer.NeuronCount; i++)
						{
							D1ErrX[i] = currentLayer.Neurons[i].Output + TrainToValue;
						}
						D1ErrX[correctAnswer] = currentLayer.Neurons[correctAnswer].Output - TrainToValue;
						break;

					case LossFunctions.NegativeLogLikelihood:
						// Not implemented yet
						break;
				}
			}

			while (currentLayer.PreviousLayer != null)
			{
				D1ErrX = currentLayer.Backpropagate(D1ErrX);
				currentLayer = currentLayer.PreviousLayer;
			}
		}

		public void EraseHessianInformation()
		{
			Parallel.ForEach(Layers.Skip(1), ParallelOption, layer =>
			{
				for (int i=0; i < layer.Weights.Count(); i++)
				{
					layer.Weights[i].DiagonalHessian = 0.0D;
				}
			});
		}

		public void DivideHessianInformationBy(double divisor)
		{
			Parallel.ForEach(Layers.Skip(1), ParallelOption, layer =>
			{
				for (int i = 0; i < layer.Weights.Count(); i++)
				{
					layer.Weights[i].DiagonalHessian /= divisor;
				}
			});
		}

		public void BackpropagateSecondDerivates()
		{
			// start the process by calculating the second derivative for the last layer.
			// for the standard MSE Err function (i.e., 0.5*sumof( (desiredOutput)^2 ), this differential is 
			// exactly one
			Layers currentLayer = Layers.Last();
			double[] D2ErrX = new double[currentLayer.NeuronCount];

			switch (currentLayer.Network.LossFunction)
			{
				case LossFunctions.CrossEntropy:
					// Not implemented yet
					break;

				case LossFunctions.MeanSquareError:
					for (int i = 0; i < currentLayer.NeuronCount; i++)
					{
						D2ErrX[i] = TrainToValue;
					}
					break;

				case LossFunctions.NegativeLogLikelihood:
					// Not implemented yet
					break;
			}

			while (currentLayer.PreviousLayer != null)
			{
				D2ErrX = currentLayer.BackpropagateSecondDerivates(D2ErrX);
				currentLayer = currentLayer.PreviousLayer; 
			}
		}
	}

	[Serializable()]
	public class Layers
	{
		public NeuralNetworks Network { get; private set; }
		public int LayerIndex { get; private set; }
		public string Name { get; private set; }
		public LayerTypes LayerType { get; private set; }
		public KernelTypes KernelType { get; private set; }
		public int NeuronCount { get; private set; }
		public int WeightCount { get; private set; }
		public bool UseMapInfo { get; private set; }
		public int MapCount { get; private set; }
		public int MapWidth { get; private set; }
		public int MapHeight { get; private set; }
		public bool IsFullyMapped { get; private set; }
		public int ReceptiveFieldWidth { get; private set; }
		public int ReceptiveFieldHeight { get; private set; }
		public double SubsamplingScalingFactor {get; private set;}
		public Layers PreviousLayer { get; private set; }
		public Layers NextLayer { get; private set; }
		public Neuron[] Neurons { get; private set; }
		public Weight[] Weights { get; private set; }
		public Mappings Mappings { get; private set; }
		public bool LockedWeights { get; set; }

		public Layers(NeuralNetworks network, LayerTypes layerType, int mapCount, int mapWidth, int mapHeight, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, KernelTypes.None, mapCount * mapWidth * mapHeight, true, mapCount, mapWidth, mapHeight, true, 0, 0, ((network.Layers.Count == 0) ? (null) : (network.Layers[network.Layers.Count - 1])), null, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int mapCount, int mapWidth, int mapHeight, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, mapCount * mapWidth * mapHeight, true, mapCount, mapWidth, mapHeight, true, 0, 0, ((network.Layers.Count == 0) ? (null) : (network.Layers[network.Layers.Count - 1])), null, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int neuronCount, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, neuronCount, false, 1, 1, 1, true, 0, 0, ((network.Layers.Count == 0) ? (null) : (network.Layers[network.Layers.Count - 1])), null, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int neuronCount, Mappings mappings, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, neuronCount, false, 1, 1, 1, false, 0, 0, ((network.Layers.Count == 0) ? (null) : (network.Layers[network.Layers.Count - 1])), mappings, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int mapCount, int mapWidth, int mapHeight, Mappings mappings, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, mapCount * mapWidth * mapHeight, true, mapCount, mapWidth, mapHeight, false, 1, 1, ((network.Layers.Count == 0) ? (null) : (network.Layers[network.Layers.Count - 1])), mappings, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int mapCount, int mapWidth, int mapHeight, int receptiveFieldWidth, int receptiveFieldHeight, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, mapCount * mapWidth * mapHeight, true, mapCount, mapWidth, mapHeight, true, receptiveFieldWidth, receptiveFieldHeight, network.Layers[network.Layers.Count - 1], null, lockedWeights) { }
		public Layers(NeuralNetworks network, LayerTypes layerType, KernelTypes kernelType, int mapCount, int mapWidth, int mapHeight, int receptiveFieldWidth, int receptiveFieldHeight, Mappings mappings, bool lockedWeights = false) : this(network, ((network.Layers.Count == 0) ? (0) : (network.Layers.Count)), layerType, kernelType, mapCount * mapWidth * mapHeight, true, mapCount, mapWidth, mapHeight, false, receptiveFieldWidth, receptiveFieldHeight, network.Layers[network.Layers.Count - 1], mappings, lockedWeights) { }
		public Layers(NeuralNetworks network, int layerIndex, LayerTypes layerType, KernelTypes kernelType, int neuronCount, bool useMapInfo, int mapCount, int mapWidth, int mapHeight, bool isFullyMapped, int receptiveFieldWidth, int receptiveFieldHeight, Layers previousLayer, Mappings mappings, bool lockedWeights = false)
		{
			Network = network;
			LayerIndex = layerIndex;
			LayerType = layerType;
			KernelType = kernelType;
			NeuronCount = neuronCount;
			UseMapInfo = useMapInfo;
			MapCount = mapCount;
			MapWidth = mapWidth;
			MapHeight = mapHeight;
			IsFullyMapped = isFullyMapped;
			ReceptiveFieldWidth = receptiveFieldWidth;
			ReceptiveFieldHeight = receptiveFieldHeight;
			PreviousLayer = previousLayer;
			LockedWeights = lockedWeights;

			
			Neurons = new Neuron[NeuronCount];
			for (int i = 0; i < NeuronCount; i++)
			{
				Neurons[i] = new Neuron();
			}

			int[] kernelTemplate;
			int iNumWeight = 0;
			int position = 0;

			switch (LayerType)
			{
				case LayerTypes.Input:
					WeightCount = 0;
					Weights = new Weight[WeightCount];
					break;

				case LayerTypes.Convolutional:
					int totalMappings;
					if (UseMapInfo)
					{
						if (IsFullyMapped)
						{
							totalMappings = PreviousLayer.MapCount * MapCount;
						}
						else
						{
							Mappings = mappings;
							if (Mappings != null)
							{
								if (Mappings.IsMapped.Count() == PreviousLayer.MapCount * MapCount)
									totalMappings = Mappings.IsMapped.Count(p => p == true);
								else
									throw new ArgumentException("Invalid mappings definition");
							}
							else
								throw new ArgumentException("Empty mappings definition");
						}
												
						WeightCount = (totalMappings * ReceptiveFieldWidth * ReceptiveFieldHeight) + MapCount;
						Weights = new Weight[WeightCount];

						kernelTemplate = new int[ReceptiveFieldWidth * ReceptiveFieldHeight];
						for (int row = 0; row < ReceptiveFieldHeight; row++)
						{
							for (int column = 0; column < ReceptiveFieldWidth; column++)
							{
								kernelTemplate[column + (row * ReceptiveFieldWidth)] = column + (row * PreviousLayer.MapWidth);
							}
						}

						int positionPrevMap = 0;
						iNumWeight = 0;
						int mapping = 0;
						int prevCurMap = -1;
						if (!IsFullyMapped) // not fully mapped
						{
							for (int curMap = 0; curMap < MapCount; ++curMap)
							{
								for (int prevMap = 0; prevMap < PreviousLayer.MapCount; ++prevMap)
								{
									positionPrevMap = prevMap * PreviousLayer.MapWidth * PreviousLayer.MapHeight;

									if (mappings.IsMapped[(curMap * PreviousLayer.MapCount) + prevMap] == true)
									{
										for (int y = 0; y < MapHeight; ++y)
										{
											for (int x = 0; x < MapWidth; ++x)
											{
												position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
												iNumWeight = (mapping * (ReceptiveFieldWidth * ReceptiveFieldHeight)) + curMap;
												if (prevCurMap != curMap)
													Neurons[position].AddBias(iNumWeight++);

												for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
												{
													Neurons[position].AddConnection(x + (y * PreviousLayer.MapWidth) + kernelTemplate[k] + positionPrevMap, iNumWeight++);
												}
											}
										}
										mapping++;
										prevCurMap = curMap;
									}
								}
							}
						}
						else // Fully mapped
						{
							if (totalMappings > MapCount)
							{
								for (int curMap = 0; curMap < MapCount; curMap++)
								{
									for (int prevMap = 0; prevMap < PreviousLayer.MapCount; prevMap++)
									{
										positionPrevMap = prevMap * PreviousLayer.MapWidth * PreviousLayer.MapHeight;

										for (int y = 0; y < MapHeight; y++)
										{
											for (int x = 0; x < MapWidth; x++)
											{
												position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
												iNumWeight = (mapping * ReceptiveFieldWidth * ReceptiveFieldHeight) + curMap;
 
												if (prevCurMap != curMap)
													Neurons[position].AddBias(iNumWeight++);

												for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
												{
													Neurons[position].AddConnection(x + (y * PreviousLayer.MapWidth) + kernelTemplate[k] + positionPrevMap, iNumWeight++);
												}
											}
										}
										mapping++;
										prevCurMap = curMap;
									}
								}
							}
							else // PreviousLayer has only one map
							{
								for (int curMap = 0; curMap < MapCount; ++curMap)
								{
									for (int y = 0; y < MapHeight; ++y)
									{
										for (int x = 0; x < MapWidth; ++x)
										{
											position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
											iNumWeight = curMap * ((ReceptiveFieldWidth * ReceptiveFieldHeight) + 1);

											Neurons[position].AddBias(iNumWeight++);

											for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
											{
												Neurons[position].AddConnection(x + (y * PreviousLayer.MapWidth) + kernelTemplate[k], iNumWeight++);
											}
										}
									}
								}
							}
						}
					}
					else
					{
						throw new ArgumentException("Inadequate mapping information provided");
					}
					break;

				case LayerTypes.ConvolutionalSubsampling:  // Simard's implementation
					if (UseMapInfo)
					{
						if (IsFullyMapped)
						{
							totalMappings = PreviousLayer.MapCount * MapCount;
						}
						else
						{
							Mappings = mappings;
							if (Mappings != null)
							{
								if (Mappings.IsMapped.Count() == PreviousLayer.MapCount * MapCount)
									totalMappings = Mappings.IsMapped.Count(p => p == true);
								else
									throw new ArgumentException("Invalid mappings definition");
							}
							else
								throw new ArgumentException("Empty mappings definition");
						}

						WeightCount = (totalMappings * ReceptiveFieldWidth * ReceptiveFieldHeight) + MapCount;
						Weights = new Weight[WeightCount];

						kernelTemplate = new int[ReceptiveFieldWidth * ReceptiveFieldHeight];
						for (int row = 0; row < ReceptiveFieldHeight; ++row)
						{
							for (int column = 0; column < ReceptiveFieldWidth; ++column)
							{
								kernelTemplate[column + (row * ReceptiveFieldWidth)] = column + (row * PreviousLayer.MapWidth);
							}
						}

						int positionPrevMap = 0;
						iNumWeight = 0;
						int mapping = 0;
						int prevCurMap = -1;
						if (!IsFullyMapped) // not fully mapped
						{
							for (int curMap = 0; curMap < MapCount; ++curMap)
							{
								for (int prevMap = 0; prevMap < PreviousLayer.MapCount; ++prevMap)
								{
									positionPrevMap = prevMap * PreviousLayer.MapWidth * PreviousLayer.MapHeight;

									if (mappings.IsMapped[(curMap * PreviousLayer.MapCount) + prevMap] == true)
									{
										for (int y = 0; y < MapHeight; ++y)
										{
											for (int x = 0; x < MapWidth; ++x)
											{
												position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
												iNumWeight = (mapping * (ReceptiveFieldWidth * ReceptiveFieldHeight)) + curMap;
												if (prevCurMap != curMap)
													Neurons[position].AddBias(iNumWeight++);

												for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
												{
													Neurons[position].AddConnection((x * 2)+ (y * 2 * PreviousLayer.MapWidth) + kernelTemplate[k] + positionPrevMap, iNumWeight++);
												}
											}
										}
										mapping++;
										prevCurMap = curMap;
									}
								}
							}
						}
						else // Fully mapped
						{
							if (totalMappings > MapCount)
							{
								for (int curMap = 0; curMap < MapCount; ++curMap)
								{
									for (int prevMap = 0; prevMap < PreviousLayer.MapCount; ++prevMap)
									{
										positionPrevMap = prevMap * PreviousLayer.MapWidth * PreviousLayer.MapHeight;

										for (int y = 0; y < MapHeight; ++y)
										{
											for (int x = 0; x < MapWidth; ++x)
											{
												position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
												iNumWeight = (mapping * ReceptiveFieldWidth * ReceptiveFieldHeight) + curMap;

												if (prevCurMap != curMap)
													Neurons[position].AddBias(iNumWeight++);

												for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
												{
													Neurons[position].AddConnection((x * 2) + (y * 2 * PreviousLayer.MapWidth) + kernelTemplate[k] + positionPrevMap, iNumWeight++);
												}
											}
										}
										mapping++;
										prevCurMap = curMap;
									}
								}
							}
							else // PreviousLayer has only one map
							{
								for (int curMap = 0; curMap < MapCount; ++curMap)
								{
									for (int y = 0; y < MapHeight; ++y)
									{
										for (int x = 0; x < MapWidth; ++x)
										{
											position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
											iNumWeight = curMap * ((ReceptiveFieldWidth * ReceptiveFieldHeight) + 1);

											Neurons[position].AddBias(iNumWeight++);

											for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
											{
												Neurons[position].AddConnection((x * 2) + (y * 2 * PreviousLayer.MapWidth) + kernelTemplate[k], iNumWeight++);
											}
										}
									}
								}
							}
						}
					}
					else
					{
						throw new ArgumentException("Inadequate mapping information provided");
					}
					break;

				case LayerTypes.Subsampling:
					if (UseMapInfo)
					{
						if (IsFullyMapped)
						{
							// Symmetrical mapping
							List<bool> mapCombinations = new List<bool>(PreviousLayer.MapCount * MapCount);
							for (int x = 0; x < MapCount; x++)
							{
								for (int y = 0; y < PreviousLayer.MapCount; y++)
								{
									mapCombinations.Add(x == y);
								}
							}
							mappings = new Mappings(network, PreviousLayer.LayerIndex, mapCombinations);
						}
						
						Mappings = mappings;
						if (Mappings != null)
						{
							if (Mappings.IsMapped.Count() == PreviousLayer.MapCount * MapCount)
								totalMappings = Mappings.IsMapped.Count(p => p == true);
							else
								throw new ArgumentException("Invalid mappings definition");
						}
						else
							throw new ArgumentException("Empty mappings definition");

						WeightCount = MapCount * 2;
						Weights = new Weight[WeightCount];

						SubsamplingScalingFactor = 1D / (receptiveFieldWidth * ReceptiveFieldHeight);

						kernelTemplate = new int[ReceptiveFieldWidth * ReceptiveFieldHeight];
						for (int row = 0; row < ReceptiveFieldHeight; ++row)
						{
							for (int column = 0; column < ReceptiveFieldWidth; ++column)
							{
								kernelTemplate[column + (row * ReceptiveFieldWidth)] = column + (row * PreviousLayer.MapWidth);
							}
						}

						int positionPrevMap = 0;
						iNumWeight = 0;
						if (PreviousLayer.MapCount > 1) //fully symmetrical mapped
						{
							for (int curMap = 0; curMap < MapCount; ++curMap)
							{
								for (int prevMap = 0; prevMap < PreviousLayer.MapCount; ++prevMap)
								{
									positionPrevMap = prevMap * PreviousLayer.MapWidth * PreviousLayer.MapHeight;

									if (mappings.IsMapped[(curMap * PreviousLayer.MapCount) + prevMap] == true)
									{
										for (int y = 0; y < MapHeight; ++y)
										{
											for (int x = 0; x < MapWidth; ++x)
											{
												position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
												iNumWeight = curMap * 2;
												Neurons[position].AddBias(iNumWeight++);
												
												for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
												{
													Neurons[position].AddConnection((x * ReceptiveFieldWidth) + (y * ReceptiveFieldHeight * PreviousLayer.MapWidth) + kernelTemplate[k] + positionPrevMap, iNumWeight);
												}
											}
										}
									}
								}
							}
						}
						else // only one previous layer
						{
							for (int curMap = 0; curMap < MapCount; ++curMap)
							{
								for (int y = 0; y < MapHeight; ++y)
								{
									for (int x = 0; x < MapWidth; ++x)
									{
										position = x + (y * MapWidth) + (curMap * MapWidth * MapHeight);
										iNumWeight = curMap * 2;

										Neurons[position].AddBias(iNumWeight++);

										for (int k = 0; k < (ReceptiveFieldWidth * ReceptiveFieldHeight); ++k)
										{
											Neurons[position].AddConnection((x * ReceptiveFieldWidth) + (y * ReceptiveFieldHeight * PreviousLayer.MapWidth) + kernelTemplate[k], iNumWeight);
										}
									}
								}
							}
						}
					}
					break;

				case LayerTypes.FullyConnected:
					WeightCount = (PreviousLayer.NeuronCount + 1) * NeuronCount;
					Weights = new Weight[WeightCount];

					iNumWeight = 0;
					if (UseMapInfo)
					{
						for (int curMap = 0; curMap < MapCount; ++curMap)
						{
							for (int yc = 0;  yc < MapHeight; ++yc)
							{
								for (int xc = 0; xc < MapWidth; ++xc)
								{
									position = xc + (yc * MapWidth) + (curMap * MapWidth * MapHeight);
									Neurons[position].AddBias(iNumWeight++);

									for (int prevMaps = 0; prevMaps < PreviousLayer.MapCount; ++prevMaps)
									{
										for (int y = 0; y < PreviousLayer.MapHeight; ++y)
										{
											for (int x = 0; x < PreviousLayer.MapWidth; ++x)
											{
												Neurons[position].AddConnection((x + (y * PreviousLayer.MapWidth) + (prevMaps * PreviousLayer.MapWidth * PreviousLayer.MapHeight)), iNumWeight++);
											}
										}
									}
								}
							}
						}
					}
					else
					{
						for (int y = 0; y < NeuronCount; ++y)
						{
							Neurons[y].AddBias(iNumWeight++);
							for (int x = 0; x < PreviousLayer.NeuronCount; ++x)
							{
								Neurons[y].AddConnection(x, iNumWeight++);
							}
						}
					}
					break;

				case LayerTypes.RBF:
					WeightCount = PreviousLayer.NeuronCount * NeuronCount; // no biasses
					Weights = new Weight[WeightCount];

					iNumWeight = 0;
					if (UseMapInfo)
					{
						for (int n = 0; n < NeuronCount; ++n)
						{
							for (int prevMaps = 0; prevMaps < PreviousLayer.MapCount; ++prevMaps)
							{
								for (int y = 0; y < PreviousLayer.MapHeight; ++y)
								{
									for (int x = 0; x < PreviousLayer.MapWidth; ++x)
									{
										Neurons[n].AddConnection((x + (y * PreviousLayer.MapWidth) + (prevMaps * PreviousLayer.MapWidth * PreviousLayer.MapHeight)), iNumWeight++);
									}
								}
							}
						}
					}
					else
					{
						for (int y = 0; y < NeuronCount; ++y)
						{
							for (int x = 0; x < PreviousLayer.NeuronCount; ++x)
							{
								Neurons[y].AddConnection(x, iNumWeight++);
							}
						}
					}
					break;
			};


			int conn = 0;
			foreach (Neuron neuron in Neurons)
			{
				conn += neuron.Connections.Count();
			}
			
			Name += "Layer: " + LayerIndex.ToString(CultureInfo.CurrentCulture) + "\r\n";
			Name += "Layer type: " + LayerType.ToString() + "\r\n" + 
				   ((KernelType != KernelTypes.None) ? ("Kernel type: " + KernelType.ToString() + "\r\n") : ("")) +
				   ((LayerType == LayerTypes.Convolutional) ? ("Receptive field: " + ReceptiveFieldWidth.ToString(CultureInfo.CurrentCulture) + "x" + ReceptiveFieldHeight.ToString(CultureInfo.CurrentCulture) + "\r\n") : "") + 
				   ((UseMapInfo) ? ("Maps: " + MapCount.ToString(CultureInfo.CurrentCulture) + "x(" + MapWidth.ToString(CultureInfo.CurrentCulture) + "x" + MapHeight.ToString(CultureInfo.CurrentCulture) + ")" + "\r\n") : ("")) +
				   "Neurons: " + NeuronCount.ToString(CultureInfo.CurrentCulture) + "\r\n" +
				   ((LayerType != LayerTypes.Input) ? ("Weights: " + Weights.Count().ToString(CultureInfo.CurrentCulture) + "\r\n") : ("")) +
				   "Connections: " + conn.ToString(CultureInfo.CurrentCulture) + "\r\n";

			if (PreviousLayer != null)
			{
				PreviousLayer.NextLayer = this;
			}
		}

		private static double Sigmoid(double value)
		{
			return (Math.Tanh(value));
			//return ((1.7159D * Math.Tanh(twodivthree * value)));
		}

		private static double DSigmoid(double value)
		{
			return (1D - Math.Pow (value, 2D));
			//return((twodivthree / 1.7159D) * (1.7159D + (value)) * (1.7159D - (value)));
		}

		private static double Gaussian(double value)
		{
			return (Math.Exp(-0.5D*value));
		}

		private static double DGaussian(double value)
		{
			return (Math.Exp(-0.5D*value));
		}

		private static double Median(List<double> values)
		{
			values.Sort();

			return (values[(values.Count-1) / 2]);
		}

		private static double GetRangeFactor(double min, double max)
		{
			if (max <= min)
				throw (new Exception("Invalid min or max parameter"));

			double range = 0D;

			if (Math.Sign(min) == Math.Sign(max))
			{
				if (Math.Sign(min) == -1)
					range = 255D / (Math.Abs(min) - Math.Abs(max));
				else
					range = 255D / (Math.Abs(max) - Math.Abs(min));
			}
			else
				range = 255D / (Math.Abs(max) + Math.Abs(min));

			return (range);
		}

		private static SolidColorBrush GetBrushFromRange(double range, double min, double max, double value)
		{
			SolidColorBrush brush = new SolidColorBrush ();
			
			//if ((value < min) || (value > max))
			//    throw (new Exception("value out of bounds"));

			if (min < 0D)
				value += Math.Abs(min);
			else
				value -= min;

			value *= range;

			brush.Color = Color.FromRgb ((byte) value, (byte)value, (byte)value);
					   
			brush.Freeze();
			return (brush);
		}

		public ScrollViewer GetMappedLayerWeights()
		{
			WrapPanel panel = new WrapPanel();
			panel.BeginInit();
			panel.UseLayoutRounding = true;
			panel.SnapsToDevicePixels = true;
			panel.Orientation = Orientation.Horizontal;
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			panel.VerticalAlignment = VerticalAlignment.Stretch;
			
			for (int map = 0; map < MapCount; map++)
			{
				Grid grid = new Grid();
				grid.SnapsToDevicePixels = true;
				grid.UseLayoutRounding = true;
				grid.Background = System.Windows.Media.Brushes.White;
				grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
				grid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
				grid.Width = double.NaN;
				grid.Height = double.NaN;
				grid.Margin = new Thickness(4);
		  
				for (int col = 0; col < MapWidth; col++)
				{
					ColumnDefinition colDef = new ColumnDefinition();
					colDef.Width = new GridLength(0, GridUnitType.Auto);
					grid.ColumnDefinitions.Add(colDef);
				}
				for (int row = 0; row < MapHeight; row++)
				{
					RowDefinition rowDef = new RowDefinition();
					rowDef.Height = new GridLength(0, GridUnitType.Auto);
					grid.RowDefinitions.Add(rowDef);
				}

				double weightMin = Weights.Min(weight => weight.Value);
				double weightMax = Weights.Max(weight => weight.Value);
				double range = GetRangeFactor(weightMin, weightMax);

				grid.BeginInit();
				for (int y = 0; y < ReceptiveFieldWidth; y++)
				{
					for (int x = 0; x < ReceptiveFieldHeight; x++)
					{
						int index = x + (y * ReceptiveFieldWidth) + (map * ((ReceptiveFieldWidth * ReceptiveFieldHeight) + 1));
						System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
						rectangle.BeginInit();
						rectangle.SnapsToDevicePixels = true;
						rectangle.UseLayoutRounding = true;
						rectangle.HorizontalAlignment = HorizontalAlignment.Stretch;
						ToolTip tip = new ToolTip();
						tip.Content = Weights[index].Value.ToString("N17", CultureInfo.CurrentUICulture);
						rectangle.ToolTip = tip;
						rectangle.VerticalAlignment = VerticalAlignment.Stretch;
						rectangle.Margin = new Thickness(0);
						rectangle.Height = 8;
						rectangle.Width = 8;
						rectangle.Stretch = Stretch.Uniform;
						rectangle.Fill = GetBrushFromRange(range, weightMin, weightMax, Weights[index].Value);
						rectangle.EndInit();
						Grid.SetColumn(rectangle, x);
						Grid.SetRow(rectangle, y);
						grid.Children.Add(rectangle);
						tip = null;
						rectangle = null;
					}
				};

				grid.EndInit();
				panel.Children.Add(grid);
			}
			panel.EndInit();

			ScrollViewer scr = new ScrollViewer();
			scr.BeginInit();
			scr.SnapsToDevicePixels = true;
			scr.UseLayoutRounding = true;
			scr.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
			scr.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			scr.HorizontalContentAlignment = HorizontalAlignment.Left;
			scr.VerticalContentAlignment = VerticalAlignment.Top;
			scr.VerticalAlignment = VerticalAlignment.Stretch;
			scr.HorizontalAlignment = HorizontalAlignment.Stretch;
			scr.Content = panel;
			scr.Height = double.NaN;
			scr.Width = double.NaN;
			scr.EndInit();
			
			return (scr);
		}

		public ScrollViewer GetMappedLayerOutputs()
		{
			WrapPanel panel = new WrapPanel();
			panel.BeginInit();
			panel.UseLayoutRounding = true;
			panel.SnapsToDevicePixels = true;
			panel.Orientation = Orientation.Horizontal;
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			panel.VerticalAlignment = VerticalAlignment.Stretch;
			
			for (int map = 0; map < MapCount; ++map)
			{
				Grid grid = new Grid();
				grid.SnapsToDevicePixels = true;
				grid.UseLayoutRounding = true;
				grid.Background = System.Windows.Media.Brushes.White;
				grid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
				grid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
				grid.Width = double.NaN;
				grid.Height = double.NaN;
				grid.Margin = new Thickness(4);

				for (int col = 0; col < MapWidth; col++)
				{
					ColumnDefinition colDef = new ColumnDefinition();
					colDef.Width = new GridLength(0, GridUnitType.Auto);
					grid.ColumnDefinitions.Add(colDef);
				}
				for (int row = 0; row < MapHeight; row++)
				{
					RowDefinition rowDef = new RowDefinition();
					rowDef.Height = new GridLength(0, GridUnitType.Auto);
					grid.RowDefinitions.Add(rowDef);
				}

				grid.BeginInit();

				for (int y = 0; y < MapHeight; y++)
				{
					for (int x = 0; x < MapWidth; x++)
					{
						int index = x + (y * MapWidth) + (map * MapWidth * MapHeight);
						byte color = (byte)(255 - ((Neurons[index].Output + 1D) * 127D));
						System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(color, color, color));
						brush.Freeze();
						System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
						rectangle.BeginInit();
						rectangle.SnapsToDevicePixels = true;
						rectangle.UseLayoutRounding = true;
						rectangle.HorizontalAlignment = HorizontalAlignment.Stretch;
						ToolTip tip = new ToolTip();
						tip.Content = Neurons[index].Output.ToString("N17", CultureInfo.CurrentUICulture);
						rectangle.ToolTip = tip;
						rectangle.VerticalAlignment = VerticalAlignment.Stretch;
						rectangle.Margin = new Thickness(0);
						rectangle.Height = 8;
						rectangle.Width = 8;
						rectangle.Stretch = Stretch.Uniform;
						rectangle.Fill = brush;
						rectangle.EndInit();
						Grid.SetColumn(rectangle, x);
						Grid.SetRow(rectangle, y);
						grid.Children.Add(rectangle);
						brush = null;
						tip = null;
						rectangle = null;
					}
				}
				grid.EndInit();
				panel.Children.Add(grid);
			}
			panel.EndInit();

			ScrollViewer scr = new ScrollViewer();
			scr.BeginInit();
			scr.SnapsToDevicePixels = true;
			scr.UseLayoutRounding = true;
			scr.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
			scr.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			scr.HorizontalContentAlignment = HorizontalAlignment.Left;
			scr.VerticalContentAlignment = VerticalAlignment.Top;
			scr.VerticalAlignment = VerticalAlignment.Stretch;
			scr.HorizontalAlignment = HorizontalAlignment.Stretch;
			scr.Content = panel;
			scr.Height = double.NaN;
			scr.Width = double.NaN;
			scr.EndInit();
			
			return (scr);
		}

		public void SetInitalWeights(bool useFannIn = true, double weightScope = 2D, double weightFactor = 1D)
		{
			if (!useFannIn)
			{
				for (int i = 0; i < WeightCount; i++)
				{
					Weights[i].Value = ((Network.RandomGenerator.NextDouble() * weightScope) - (weightScope / 2D)) * weightFactor;
				}
			}
			else
			{
				switch (LayerType)
				{
					case  LayerTypes.RBF:
						int index = 0;
						Parallel.ForEach(Neurons, Network.ParallelOption, neuron => //10x
						{
							byte[] weightImage = new byte[12];
							weightImage = Network.RbfWeights[index].ToArray();
							double[] realWeights = new double[(7 * 12)];
							int row;
							for (int y = 0; y < 12; y++)
							{
								row = (int)weightImage[y];
								realWeights[0 + (7 * y)] = (((128 & ~row) / 128) * 2) - 1;
								realWeights[1 + (7 * y)] = (((64 & ~row) / 64) * 2) - 1;
								realWeights[2 + (7 * y)] = (((32 & ~row) / 32) * 2) - 1;
								realWeights[3 + (7 * y)] = (((16 & ~row) / 16) * 2) - 1;
								realWeights[4 + (7 * y)] = (((8 & ~row) / 8) * 2) - 1;
								realWeights[5 + (7 * y)] = (((4 & ~row) / 4) * 2) - 1;
								realWeights[6 + (7 * y)] = (((2 & ~row) / 2) * 2) - 1;
							}

							foreach (Connection connection in neuron.Connections) //84x
							{
								if (connection.ToNeuronIndex != int.MaxValue)
									Weights[connection.ToWeightIndex].Value = (realWeights[connection.ToNeuronIndex] == 1D) ? Network.TrainToValue : -Network.TrainToValue;
								else
									Weights[connection.ToWeightIndex].Value = 0.005;
							}
							index++;
						});
						break;
					
					default:
						int windowNeuronCount = 0;
						foreach (Connection connection in Neurons[0].Connections)
						{
							if (connection.ToNeuronIndex != int.MaxValue)
								windowNeuronCount++;
						}

						//weightFactor = Math.Pow((double)windowNeuronCount, -0.5D);
						weightFactor = 1D;
						weightScope = 1D / Math.Sqrt(((double)windowNeuronCount));
						Parallel.ForEach(Neurons, Network.ParallelOption, neuron =>
						{
							foreach (Connection connection in neuron.Connections)
							{
									Weights[connection.ToWeightIndex].Value = ((Network.RandomGenerator.NextDouble() * weightScope) - (weightScope / 2D)) * weightFactor;
							}
						});
						break;
				}
			}
		}

		public void Calculate()
		{
			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
					Parallel.ForEach (Neurons, Network.ParallelOption, neuron =>
					{
						double dSum = 0D;
						foreach (Connection connection in neuron.Connections)
						{
							if (connection.ToNeuronIndex == int.MaxValue)
								dSum += Weights[connection.ToWeightIndex].Value;
							else
								dSum += Weights[connection.ToWeightIndex].Value * PreviousLayer.Neurons[connection.ToNeuronIndex].Output;
						}
						neuron.Output = Sigmoid(dSum);
					});
					break;

				case LayerTypes.Subsampling:
					switch (KernelType)
					{
						case KernelTypes.AveragePooling:
							Parallel.ForEach(Neurons, Network.ParallelOption,neuron =>
							{
								double dSum = 0D;
								foreach (Connection connection in neuron.Connections)
								{
									if (connection.ToNeuronIndex == int.MaxValue)
										dSum += Weights[connection.ToWeightIndex].Value;
									else
										dSum += Weights[connection.ToWeightIndex].Value * PreviousLayer.Neurons[connection.ToNeuronIndex].Output * SubsamplingScalingFactor;
								}
								neuron.Output = Sigmoid(dSum);
							});
							break;

						case KernelTypes.MaxPooling:
							Parallel.ForEach(Neurons, Network.ParallelOption, neuron =>
							{
								double bias = 0D;
								double weight = 1D;
								List<double> previousOutputs = new List<double>(4);
								foreach (Connection connection in neuron.Connections)
								{
									
									if (connection.ToNeuronIndex == int.MaxValue)
										bias = Weights[connection.ToWeightIndex].Value;
									else
									{
										weight = Weights[connection.ToWeightIndex].Value;
										previousOutputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}
								neuron.Output = Sigmoid((previousOutputs.Max() * weight) + bias);
							});
							break;

						case KernelTypes.MedianPooling:
							Parallel.ForEach(Neurons, Network.ParallelOption, neuron =>
							{
								double bias = 0D;
								double weight = 1D;
								List<double> Outputs = new List<double>(3);
								List<double> Q = new List<double>(3);
								foreach (Connection connection in neuron.Connections)
								{

									if (connection.ToNeuronIndex == int.MaxValue)
										bias = Weights[connection.ToWeightIndex].Value;
									else
									{
										weight = Weights[connection.ToWeightIndex].Value;
										Outputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}

								List<double> rOutputs = new List<double>();
								foreach (double output in Outputs)
								{
									rOutputs.Add(output);
								}

								foreach (double output in Outputs)
								{
									rOutputs.Remove(output);
									Q.Add(Math.Pow(output - Median(rOutputs), 2D));
									rOutputs.Add(output);
								}

								int bestIndex = Q.IndexOf(Q.Min());

								neuron.Output = Sigmoid((Outputs[bestIndex] * weight) + bias);
							});
							break;
					}
					break;

				case LayerTypes.RBF:
					Parallel.ForEach(Neurons, Network.ParallelOption, neuron =>
					{
						double dSum = 0;
						foreach (Connection connection in neuron.Connections)
						{
							dSum += Math.Pow((PreviousLayer.Neurons[connection.ToNeuronIndex].Output - Weights[connection.ToWeightIndex].Value), 2);
						}
						neuron.Output = Gaussian(dSum);
					});
					break;
			}
		}

		public double[] Backpropagate(double[] neuronD1ErrX)
		{
			double[] neuronD1ErrY = new double[NeuronCount];
			double[] weightD1Err = new double[WeightCount];
			double[] prevNeuronsD1ErrX = new double[PreviousLayer.NeuronCount];

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
				case LayerTypes.Subsampling:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						neuronD1ErrY[i] = DSigmoid(Neurons[i].Output) * neuronD1ErrX[i];
					});
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, i =>
					{
						neuronD1ErrY[i] = DGaussian(Neurons[i].Output) * neuronD1ErrX[i];
					});
					break;
			}

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex == int.MaxValue)
								weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i];
							else
								weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i] * PreviousLayer.Neurons[connection.ToNeuronIndex].Output;
						}
					});
					break;

				case LayerTypes.Subsampling:
					switch (KernelType)
					{
						case KernelTypes.AveragePooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								foreach (Connection connection in Neurons[i].Connections)
								{
									if (connection.ToNeuronIndex == int.MaxValue)
										weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i];
									else
										weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i] * PreviousLayer.Neurons[connection.ToNeuronIndex].Output * SubsamplingScalingFactor;
								}
							});
							break;

						case KernelTypes.MaxPooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								int weightIndex = 1;
								List<double> previousOutputs = new List<double>(4);
								foreach (Connection connection in Neurons[i].Connections)
								{
									if (connection.ToNeuronIndex == int.MaxValue)
										weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i];
									else
									{
										weightIndex = connection.ToWeightIndex;
										previousOutputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}
								weightD1Err[weightIndex] += neuronD1ErrY[i] * previousOutputs.Max();
							});
							break;

						case KernelTypes.MedianPooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								int weightIndex = 1;
								List<double> Outputs = new List<double>(4);
								List<double> Q = new List<double>(4);
								foreach (Connection connection in Neurons[i].Connections)
								{

									if (connection.ToNeuronIndex == int.MaxValue)
										weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i];
									else
									{
										weightIndex = connection.ToWeightIndex;
										Outputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}

								List<double> rOutputs = new List<double>();
								foreach (double output in Outputs)
								{
									rOutputs.Add(output);
								}

								foreach (double output in Outputs)
								{
									rOutputs.Remove(output);
									Q.Add(Math.Pow(output - Median(rOutputs), 2D));
									rOutputs.Add(output);
								}
								int bestIndex = Q.IndexOf(Q.Min());

								weightD1Err[weightIndex] += neuronD1ErrY[i] * Outputs[bestIndex];
							});
							break;
					}
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex == int.MaxValue)
								weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i];
							else
								weightD1Err[connection.ToWeightIndex] += neuronD1ErrY[i] * (PreviousLayer.Neurons[connection.ToNeuronIndex].Output - Weights[connection.ToWeightIndex].Value);
						}
					});
					break;
			}

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
				case LayerTypes.Subsampling:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex != int.MaxValue)
								prevNeuronsD1ErrX[connection.ToNeuronIndex] += neuronD1ErrY[i] * Weights[connection.ToWeightIndex].Value;
						}
					});
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex != int.MaxValue)
								prevNeuronsD1ErrX[connection.ToNeuronIndex] += neuronD1ErrY[i] * (PreviousLayer.Neurons[connection.ToNeuronIndex].Output - Weights[connection.ToWeightIndex].Value);
						}
					});
					break;
			}

			if (!LockedWeights)
				Parallel.For(0, WeightCount, Network.ParallelOption, i =>
				{
					Weights[i].Value -= Network.TrainingRate.Rate / (Weights[i].DiagonalHessian + Network.dMicron) * weightD1Err[i];
				});
			
			return (prevNeuronsD1ErrX);
		}

		public double[] BackpropagateSecondDerivates(double[] neuronD2ErrX)
		{
			double[] neuronD2ErrY = new double[NeuronCount];
			double[] weightD2Err = new double[WeightCount];
			double[] prevNeuronD2ErrX = new double[PreviousLayer.NeuronCount];

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
				case LayerTypes.Subsampling:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						neuronD2ErrY[i] = Math.Pow(DSigmoid(Neurons[i].Output), 2D) * neuronD2ErrX[i];
					});
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, i =>
					{
						neuronD2ErrY[i] = Math.Pow(DGaussian(Neurons[i].Output), 2D) * neuronD2ErrX[i];
					});
					break;
			}

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex == int.MaxValue)
								weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i];
							else
								weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i] * PreviousLayer.Neurons[connection.ToNeuronIndex].Output * PreviousLayer.Neurons[connection.ToNeuronIndex].Output;
						}
					});
					break;

				case LayerTypes.Subsampling:
					switch (KernelType)
					{
						case KernelTypes.AveragePooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								foreach (Connection connection in Neurons[i].Connections)
								{
									if (connection.ToNeuronIndex == int.MaxValue)
										weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i];
									else
										weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i] * Math.Pow(PreviousLayer.Neurons[connection.ToNeuronIndex].Output * SubsamplingScalingFactor, 2D);
								}
							});
							break;


						case KernelTypes.MaxPooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								int weightIndex = 1;
								List<double> previousOutputs = new List<double>(4);
								foreach (Connection connection in Neurons[i].Connections)
								{
									if (connection.ToNeuronIndex == int.MaxValue)
										weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i];
									else
									{
										weightIndex = connection.ToWeightIndex;
										previousOutputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}
								weightD2Err[weightIndex] += neuronD2ErrY[i] * Math.Pow(previousOutputs.Max(), 2D);
							});
							break;

						case KernelTypes.MedianPooling:
							Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
							{
								int weightIndex = 1;
								List<double> Outputs = new List<double>(3);
								List<double> Q = new List<double>(3);
								foreach (Connection connection in Neurons[i].Connections)
								{

									if (connection.ToNeuronIndex == int.MaxValue)
										weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i];
									else
									{
										weightIndex = connection.ToWeightIndex;
										Outputs.Add(PreviousLayer.Neurons[connection.ToNeuronIndex].Output);
									}
								}
								List<double> rOutputs = new List<double>();
								foreach (double output in Outputs)
								{
									rOutputs.Add(output);
								}

								foreach (double output in Outputs)
								{
									rOutputs.Remove(output);
									Q.Add(Math.Pow(output - Median(rOutputs), 2D));
									rOutputs.Add(output);
								}
								int bestIndex = Q.IndexOf(Q.Min());

								weightD2Err[weightIndex] += neuronD2ErrY[i] * Math.Pow(Outputs[bestIndex], 2D);
							});
							break;
					}
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex == int.MaxValue)
								weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i];
							else
								weightD2Err[connection.ToWeightIndex] += neuronD2ErrY[i] * Math.Pow(PreviousLayer.Neurons[connection.ToNeuronIndex].Output - Weights[connection.ToWeightIndex].Value, 2D);
						}
					});
					break;
			}

			switch (LayerType)
			{
				case LayerTypes.Convolutional:
				case LayerTypes.ConvolutionalSubsampling:
				case LayerTypes.FullyConnected:
				case LayerTypes.Subsampling:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex != int.MaxValue)
								prevNeuronD2ErrX[connection.ToNeuronIndex] += neuronD2ErrY[i] * Weights[connection.ToWeightIndex].Value * Weights[connection.ToWeightIndex].Value;
						}
					});
					break;

				case LayerTypes.RBF:
					Parallel.For(0, NeuronCount, Network.ParallelOption, i =>
					{
						foreach (Connection connection in Neurons[i].Connections)
						{
							if (connection.ToNeuronIndex != int.MaxValue)
								prevNeuronD2ErrX[connection.ToNeuronIndex] += neuronD2ErrY[i] * Math.Pow(PreviousLayer.Neurons[connection.ToNeuronIndex].Output - Weights[connection.ToWeightIndex].Value, 2D);
						}
					});
					break;
			}

			for (int i = 0; i < WeightCount; i++)
			{
				Weights[i].DiagonalHessian += weightD2Err[i];
			}
			
			return (prevNeuronD2ErrX);
		}
	}
}