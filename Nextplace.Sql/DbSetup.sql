	

create table dbo.Property (
	id bigint identity (1,1) primary key not null,
	[propertyId] [nvarchar](450) NOT NULL,
	[nextplaceId] [nvarchar](450) NOT NULL,
	[listingId] [nvarchar](450) NOT NULL,
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
	[active] [bit] NOT NULL,)
go

create table dbo.Miner (
	id bigint identity (1,1) primary key not null, 
	hotKey nvarchar(100) not null,
	coldKey nvarchar(100) not null,
	incentive float(53) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
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

create table dbo.PropertyPrediction (
	id bigint identity (1,1) primary key not null,
	propertyId bigint foreign key references dbo.Property (id) not null,
	minerId bigint foreign key references dbo.Miner (id) not null, 
	predictionDate datetime2 not null,
	predictedSaleDate datetime2 not null,
	predictedSalePrice float(53) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go  

create table dbo.MinerStats (
	id bigint identity (1,1) primary key not null,
	minerId bigint foreign key references dbo.Miner (id) not null,  
	createDate datetime2 not null, 
	statType nvarchar(450) not null,
	ranking int not null,
	numberOfPredictions int not null,
	correctPredictions int not null)
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