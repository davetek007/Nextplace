	

create table dbo.Property (
	id bigint identity (1,1) primary key not null,
	longitude float(53) not null,
	latitude float(53) not null,
	market nvarchar(450) not null,
	city nvarchar(450) not null, 
	state nvarchar(450) not null, 
	zipCode nvarchar(450) not null, 
	address nvarchar(450) not null, 
	listingDate datetime2 not null,
	listingPrice float(53) not null,
	saleDate datetime2 not null,
	salePrice float(53) not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
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