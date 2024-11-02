create table dbo.Market (
	id int identity(1,1) primary key not null, 
	name nvarchar(450) not null, 
	externalId nvarchar(450) not null,
	[createDate] [datetime2](7) NOT NULL,
	[lastUpdateDate] [datetime2](7) NOT NULL,
	[active] [bit] NOT NULL)
go

create table dbo.Property (
	id bigint identity (1,1) primary key not null,
	[propertyId] bigint NOT NULL,
	[nextplaceId] [nvarchar](450) NOT NULL,
	[listingId] bigint NOT NULL,
	[longitude] [float] NOT NULL,
	[latitude] [float] NOT NULL,
	[market] [nvarchar](450) NOT NULL,
	[city] [nvarchar](450) NULL,
	[state] [nvarchar](450) NULL,
	[zipCode] [nvarchar](450) NULL,
	[address] [nvarchar](450) NULL,
	[listingDate] [datetime2](7) NOT NULL,
	[listingPrice] [float] NOT NULL,
	[numberOfBeds] [int] NULL,
	[numberOfBaths] [float] NULL,
	[squareFeet] [int] NULL,
	[lotSize] bigint  NULL,
	[yearBuilt] [int] NULL,
	[propertyType] [nvarchar](450) NOT NULL,
	[lastSaleDate] [datetime2](7) NULL,
	[hoaDues] [int] NULL,
	[saleDate] [datetime2](7) NULL,
	[salePrice] [float] NULL,
	[createDate] [datetime2](7) NOT NULL,
	[lastUpdateDate] [datetime2](7) NOT NULL,
	[active] [bit] NOT NULL,
	estimatesCollected bit not null)
go

create table dbo.PropertyEstimate (
	id bigint identity (1,1) primary key not null, 
	propertyId bigint foreign key references dbo.Property (id) not null,
	dateEstimated datetime2 not null,
	estimate float(53) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

create table dbo.FunctionLog (
	id bigint identity (1,1) primary key not null,
	functionName nvarchar(450) not null,	
	logEntry nvarchar(max) not null,	
	entryType nvarchar(450) not null,	
	timeStamp datetime2(7) not null,
	executionInstanceId nvarchar(450) not null)
go
	
create table dbo.Validator (
	id bigint identity (1,1) primary key not null, 
	hotKey nvarchar(100) not null,
	coldKey nvarchar(100) not null,
	incentive float(53) not null,
	ipAddress nvarchar(450) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

CREATE TABLE [dbo].[Miner](
	id bigint identity (1,1) primary key not null,
	[hotKey] [nvarchar](100) NOT NULL,
	[coldKey] [nvarchar](100) NOT NULL,
	uid int NOT NULL,
	[incentive] [float] NOT NULL,
	[createDate] [datetime2](7) NOT NULL,
	[lastUpdateDate] [datetime2](7) NOT NULL,
	[active] [bit] NOT NULL)
go

create table dbo.MinerScore (
	id bigint identity (1,1) primary key not null,
	minerId bigint foreign key references dbo.Miner (id) not null, 
	validatorId bigint foreign key references dbo.Validator (id), 
	score float(53) not null,
	numPredictions int not null,
	scoreGenerationDate datetime2 not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go  

create table dbo.PropertyPrediction (
	id bigint identity (1,1) primary key not null,
	minerId bigint foreign key references dbo.Miner (id) not null, 
	validatorId bigint foreign key references dbo.Validator (id),
	predictionDate datetime2 not null,
	predictedSaleDate datetime2 not null,
	predictedSalePrice float(53) not null,
	predictionScore float(53) null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go  

create table dbo.PropertyEstimateStats (
	id bigint identity (1,1) primary key not null, 
	propertyId bigint foreign key references dbo.Property (id) not null,
	firstEstimateDate datetime2 not null,
	lastEstimateDate datetime2 not null,
	firstEstimateAmount float(53) not null,
	lastEstimateAmount float(53) not null,
	numEstimate int not null,
	minEstimate float(53) not null,
	maxEstimate float(53) not null,
	avgEstimate float(53) not null,
	closestEstimate float(53) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

create table dbo.ApiLog (
	id bigint identity (1,1) primary key not null,
	apiName nvarchar(450) not null,	
	logEntry nvarchar(max) not null,	
	entryType nvarchar(450) not null,	
	timeStamp datetime2(7) not null,
	executionInstanceId nvarchar(450) not null)
go

create nonclustered index ixnMinerHotKeyColdKey on dbo.Miner(hotKey, coldKey)
go

create nonclustered index ixnPropertyListingIdPropertyId on dbo.Property (listingId, propertyId)
go

create nonclustered index ixnPropertyNextplaceId on dbo.Property (nextplaceId) include (listingDate)
go

CREATE INDEX IX_Property_SaleDate_Active ON Property (SaleDate, Id, Active);
CREATE INDEX IX_PropertyPrediction_PropertyId_Active ON PropertyPrediction (PropertyId, Active);
CREATE INDEX IX_Property_ListingDate_Id ON Property (ListingDate, Id);
CREATE INDEX IX_PropertyPrediction_PropertyId_Id ON PropertyPrediction (PropertyId, Id);
CREATE INDEX IX_Property_Covering ON Property (Id, SaleDate, ListingDate, Active);
CREATE INDEX IX_PropertyPrediction_PredictionData ON PropertyPrediction (PropertyId, Active, PredictedSaleDate, PredictedSalePrice);
CREATE INDEX IX_PropertyEstimate_PropertyId_Estimate ON PropertyEstimate (PropertyId, Estimate);
go