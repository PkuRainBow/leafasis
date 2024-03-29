USE [NeuralNetwork]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LossFunctions]
(
	[LossFunction] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nchar](50) NOT NULL,
	CONSTRAINT [PK_LossFunctions] PRIMARY KEY CLUSTERED ([LossFunction] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LayerTypes]
(
	[LayerType] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nchar](50) NOT NULL,
	CONSTRAINT [PK_LayerTypes] PRIMARY KEY CLUSTERED ([LayerType] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KernelTypes]
(
	[KernelType] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nchar](50) NOT NULL,
	CONSTRAINT [PK_KernelTypes] PRIMARY KEY CLUSTERED ([KernelType] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
)  ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NeuralNetworks]
(
	[NetworkId] [uniqueidentifier] ROWGUIDCOL NOT NULL,
	[Name] [nchar](50) NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[DMicron] [float](53) NOT NULL,
	[TrainToValue] [float](53) NOT NULL,
	[LossFunction] [int] NOT NULL,
	[MaxEpochs] [int] NOT NULL,
	CONSTRAINT [PK_NeuralNetworks] PRIMARY KEY CLUSTERED ([NetworkId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_NeuralNetworks_LossFunctions] FOREIGN KEY ([LossFunction]) REFERENCES [dbo].[LossFunctions] ([LossFunction])
)  ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Layers]
(
	[NetworkId] [uniqueidentifier] NOT NULL,
	[LayerIndex] [int] NOT NULL,
	[LayerType] [int] NOT NULL,
	[KernelType] [int] NOT NULL,
	[NeuronCount] [int] NOT NULL,
	[UseMapInfo] [bit] NOT NULL,
	[MapCount] [int] NOT NULL,
	[MapWidth] [int] NOT NULL,
	[MapHeight] [int] NOT NULL,
	[IsFullyMapped] [bit] NOT NULL,
	[ReceptiveFieldWidth] [int] NULL,
	[ReceptiveFieldHeight] [int] NULL,
	[LockedWeights] [bit] NOT NULL,
	CONSTRAINT [PK_Layers] PRIMARY KEY CLUSTERED ([NetworkId], [LayerIndex] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_Layers_NeuralNetworks] FOREIGN KEY ([NetworkId]) REFERENCES [dbo].[NeuralNetworks] ([NetworkId]),
	CONSTRAINT [FK_Layers_LayerTypes] FOREIGN KEY ([LayerType]) REFERENCES [dbo].[LayerTypes] ([LayerType]),
	CONSTRAINT [FK_Layers_KernelTypes] FOREIGN KEY ([KernelType]) REFERENCES [dbo].[KernelTypes] ([KernelType])
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Neurons]
(
	[NetworkId] [uniqueidentifier] NOT NULL,
	[LayerIndex] [int] NOT NULL,
	[NeuronIndex] [int] NOT NULL,
	[Output] [float](53) NULL,
	CONSTRAINT [PK_Neurons] PRIMARY KEY CLUSTERED ([NetworkId], [LayerIndex] ASC, [NeuronIndex] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_Neurons_Layers] FOREIGN KEY ([NetworkId], [LayerIndex]) REFERENCES [dbo].[Layers] ([NetworkId], [LayerIndex])
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Connections]
(
	[NetworkId] [uniqueidentifier] NOT NULL,
	[LayerIndex] [int] NOT NULL,
	[NeuronIndex] [int] NOT NULL,
	[ConnectionIndex] [int] NOT NULL,
	[ToNeuronIndex] [int] NOT NULL,
	[ToWeightIndex] [int] NOT NULL,
	CONSTRAINT [PK_Connections] PRIMARY KEY CLUSTERED ([NetworkId] , [LayerIndex] ASC, [NeuronIndex] ASC, [ConnectionIndex] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_Connections_Neurons] FOREIGN KEY ([NetworkId], [LayerIndex], [NeuronIndex]) REFERENCES [dbo].[Neurons] ([NetworkId], [LayerIndex], [NeuronIndex])
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Mappings]
(
	[NetworkId] [uniqueidentifier] NOT NULL,
	[LayerIndex] [int] NOT NULL,
	[PreviousMapIndex] [int] NOT NULL,
	[CurrentMapIndex] [int] NOT NULL,
	[IsMapped] [bit] NOT NULL,
	CONSTRAINT [PK_Mappings] PRIMARY KEY CLUSTERED ([NetworkId], [LayerIndex] ASC, [PreviousMapIndex] ASC, [CurrentMapIndex] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_Mappings_Layers] FOREIGN KEY ([NetworkId], [LayerIndex]) REFERENCES [dbo].[Layers] ([NetworkId], [LayerIndex])
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Weights]
(
	[NetworkId] [uniqueidentifier] NOT NULL,
	[LayerIndex] [int] NOT NULL,
	[WeightIndex] [int] NOT NULL,
	[Value] [float](53) NULL,
	[DiagonalHessian] [float](53) NULL,
	CONSTRAINT [PK_Weights] PRIMARY KEY CLUSTERED ([NetworkId], [LayerIndex] ASC, [WeightIndex] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	CONSTRAINT [FK_Weights_Layers] FOREIGN KEY ([NetworkId], [LayerIndex]) REFERENCES [dbo].[Layers] ([NetworkId], [LayerIndex])
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrainingRates]
(
	[Rate] [float](53) NOT NULL,
	[Epochs] [int] NOT NULL,
	[MinimumRate] [float](53) NOT NULL,
	[DecayFactor] [float](53) NOT NULL,
	[DecayAfterEpochs] [int] NOT NULL,
	[WeightSaveTreshold] [float](53) NOT NULL,
	[Distorted] [bit] NOT NULL,
	[SameDistortionsForEpochs] [int] NOT NULL,
	[SeverityFactor] [float](53) NOT NULL,
	[MaxScaling] [float](53) NOT NULL,
	[MaxRotation] [float](53) NOT NULL,
	[ElasticSigma] [float](53) NOT NULL,
	[ElasticScaling] [float](53) NOT NULL,
	) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MNISTTraining]
(
	[Index] [int] IDENTITY(0,1) NOT NULL,
	[Label] [int] NOT NULL,
	[Image] [varbinary](784) NOT NULL,
	CONSTRAINT [PK_MNISTTraining] PRIMARY KEY CLUSTERED ([Index] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MNISTTesting]
(
	[Index] [int] IDENTITY(0,1) NOT NULL,
	[Label] [int] NOT NULL,
	[Image] [varbinary](784) NOT NULL,
	CONSTRAINT [PK_MNISTTesting] PRIMARY KEY CLUSTERED ([Index] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
--CREATE TABLE [dbo].[TrainingPatterns]
--(
--	[Index] [int] NOT NULL,
--	[Label] [int] NOT NULL,
--	[Y] [int] NOT NULL,
--	[X] [int] NOT NULL,
--	[GreyLevel] [float](53) NOT NULL,
--	CONSTRAINT [PK_TrainingPatterns] PRIMARY KEY CLUSTERED ([Index] ASC, [Y] ASC, [X] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
--) ON [PRIMARY]
--GO


--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
--CREATE TABLE [dbo].[TestingPatterns]
--(
--	[Index] [int] NOT NULL,
--	[Label] [int] NOT NULL,
--	[Y] [int] NOT NULL,
--	[X] [int] NOT NULL,
--	[GreyLevel] [float](53) NOT NULL,
--	CONSTRAINT [PK_TestingPatterns] PRIMARY KEY CLUSTERED ([Index] ASC, [Y] ASC, [X] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
--) ON [PRIMARY]
--GO