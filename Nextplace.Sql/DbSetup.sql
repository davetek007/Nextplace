create table dbo.Market (
	id int identity(1,1) primary key not null, 
	name nvarchar(450) not null, 
	country [nvarchar](450) NULL,
	externalId nvarchar(450) not null,
	[createDate] [datetime2](7) NOT NULL,
	[lastUpdateDate] [datetime2](7) NOT NULL,
	[active] [bit] NOT NULL)
go

create table dbo.PropertyValuation (
	id bigint identity (1,1) primary key not null,
	[nextplaceId] [nvarchar](450) NOT NULL,
	[longitude] [float] NOT NULL,
	[latitude] [float] NOT NULL,
	[city] [nvarchar](450) NULL,
	[state] [nvarchar](450) NULL,
	[zipCode] [nvarchar](450) NULL,
	country [nvarchar](450) NULL,
	[address] [nvarchar](450) NULL,
	[numberOfBeds] [int] NULL,
	[numberOfBaths] [float] NULL,
	[squareFeet] [int] NULL,
	[lotSize] bigint  NULL,
	[yearBuilt] [int] NULL,
	[hoaDues] [int] NULL,
	propertyType int not null,
	proposedListingPrice [float] not null,
	estimatedListingPrice [float] not null,
	requestorEmailAddress [nvarchar](450) NULL,
	requestStatus [nvarchar](450) NULL,
	[createDate] [datetime2](7) NOT NULL,
	[lastUpdateDate] [datetime2](7) NOT NULL,
	[active] [bit] NOT NULL)
go
	
create table dbo.[User] (
	id bigint identity (1,1) primary key not null,
	password nvarchar(450) not null,
	salt nvarchar(450) not null,
	emailAddress nvarchar(450) not null,
	validationKey nvarchar(450) null, 
	sessionToken nvarchar(450) null, 
	status nvarchar(450) not null,
	userType nvarchar(450) null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

create table dbo.UserFavorite (
	id bigint identity (1,1) primary key not null, 
	userId bigint foreign key references dbo.[User] (id) not null,	
	[nextplaceId] [nvarchar](450) NOT NULL,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

create table dbo.UserSetting (
	id bigint identity (1,1) primary key not null, 
	userId bigint foreign key references dbo.[User] (id) not null,	
	settingName [nvarchar](450) NOT NULL,
	settingValue [nvarchar](max) NOT NULL,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
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
	country [nvarchar](450) NULL,
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

create table dbo.PropertyImage (
	id bigint identity (1,1) primary key not null, 
	propertyId bigint foreign key references dbo.Property (id) not null,
	imageId nvarchar (450) not null, 
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go

create table dbo.PropertyShare (
	id bigint identity (1,1) primary key not null, 
	propertyId bigint foreign key references dbo.Property (id) not null,
	senderEmailAddress nvarchar (450) not null, 
	receiverEmailAddress nvarchar (450) not null, 
	message nvarchar (max) null, 
	shareRef nvarchar (450) not null,
	viewCount int not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
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
	appVersion nvarchar(450) null,
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
	totalPredictions int not null,
	scoreGenerationDate datetime2 not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go  

create table dbo.MinerDatedScore (
	id bigint identity (1,1) primary key not null,
	minerScoreId bigint foreign key references dbo.MinerScore (id) not null, 
	date datetime2 not null, 
	totalScored int not null,
	createDate datetime2 not null,
	lastUpdateDate datetime2 not null,
	active bit not null)
go  

create table dbo.PropertyPrediction (
	id bigint identity (1,1) primary key not null,
	propertyId bigint foreign key references dbo.Property (id) not null, 
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

create table dbo.PropertyValuationPrediction (
	id bigint identity (1,1) primary key not null,
	propertyValuationId bigint foreign key references dbo.PropertyValuation (id) not null, 
	minerId bigint foreign key references dbo.Miner (id) not null, 
	validatorId bigint foreign key references dbo.Validator (id),
	predictionDate datetime2 not null,
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

create table dbo.PropertyPredictionStats (
	id bigint identity (1,1) primary key not null, 
	propertyId bigint foreign key references dbo.Property (id) not null,
	numPredictions int not null,
	avgPredictedSalePrice float(53) not null,
	minPredictedSalePrice float(53) not null,
	maxPredictedSalePrice float(53) not null,
	top10Predictions nvarchar(max) not null,
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
	executionInstanceId nvarchar(450) not null,
	ipAddress nvarchar(450) null)
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

create procedure [dbo].[CalculatePropertyEstimateStats] (@executionInstanceId nvarchar(450))
as
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Stored Procedure started', 'Information', getutcdate(), @executionInstanceId)

	select		*
	into		#e
	from		dbo.PropertyEstimate e
	where		active = 0x1
	and			propertyId not in (
		select		propertyId
		from		dbo.PropertyEstimateStats
		where		createDate > dateadd (hh, -1, getutcdate())
		and			active = 0x1)
	and			propertyId in (
		select		propertyId 
		from (
			select		propertyId, max (dateEstimated) as maxDateEstimated
			from		dbo.PropertyEstimate
			group by	propertyId
			having		max (dateEstimated) > dateadd (dd, -3, getutcdate())) as a)
			
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Estimated selected', 'Information', getutcdate(), @executionInstanceId)

	select		*
	into		#p
	from		dbo.Property
	where		active = 0x1
	and			id in (select propertyId from #e)
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Properties selected', 'Information', getutcdate(), @executionInstanceId)

	delete		e
	from		#p p, #e e
	where		p.id = e.propertyId
	and			p.saleDate is not null
	and			e.dateEstimated >= p.saleDate	

	delete		p
	from		#p p 
	where		p.id not in (select propertyId from #e)
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Estimates beyond sale date removed', 'Information', getutcdate(), @executionInstanceId)

	select		e.propertyId, 
				min (dateEstimated) as firstEstimateDate, max (dateEstimated) as lastEstimateDate,
				count (1) as numEstimates, avg (estimate) as avgEstimate, min (estimate) as minEstimate, max (estimate) as maxEstimate,
				cast (null as float) as firstEstimateAmount,
				cast (null as float) as lastEstimateAmount,
				cast (null as float) as closestEstimate
	into		#s
	from		#e e 
	group by	e.propertyId
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Stats calculated', 'Information', getutcdate(), @executionInstanceId)

	update		s
	set			s.closestEstimate = e.closestEstimate
	from		#s s, (
		select		min(e1.estimate) as closestEstimate, e1.propertyId
		from		#e e1, (
			select		e.propertyId, p.salePrice, min(abs (e.Estimate - p.salePrice)) as dist
			from		#e e, #p p
			where		e.propertyId = p.id 
			group by	e.propertyId, p.salePrice) as e2
		where		e1.propertyId = e2.propertyId
		and			abs (e1.Estimate - e2.salePrice) = e2.dist
		group by	e1.propertyId) e
	where	s.propertyId = e.propertyId

	update		s
	set			s.firstEstimateAmount = e.firstEstimateAmount
	from		#s s, (
		select		e1.estimate as firstEstimateAmount, e1.propertyId
		from		#e e1, (
			select		min (id) as firstEstimateId, propertyId
			from		#e 
			group by	propertyId) e2
		where		e1.id = e2.firstEstimateId) as e
	where		s.propertyId = e.propertyId

	update		s
	set			s.lastEstimateAmount = e.lastEstimateAmount
	from		#s s, (
		select		e1.estimate as lastEstimateAmount, e1.propertyId
		from		#e e1, (
			select		max (id) as lastEstimateId, propertyId
			from		#e 
			group by	propertyId) e2
		where		e1.id = e2.lastEstimateId) as e
	where		s.propertyId = e.propertyId
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Closest, first and last stats calculated', 'Information', getutcdate(), @executionInstanceId)

	delete	s
	from	dbo.PropertyEstimateStats s
	where	s.propertyId in (select propertyId from #s)
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Old entries deleted', 'Information', getutcdate(), @executionInstanceId)

	insert	dbo.PropertyEstimateStats (propertyId, firstEstimateDate, lastEstimateDate, firstEstimateAmount, lastEstimateAmount, numEstimates, minEstimate, maxEstimate, avgEstimate, closestEstimate, createDate, lastUpdateDate, active)
	select	propertyId, firstEstimateDate, lastEstimateDate, firstEstimateAmount, lastEstimateAmount, numEstimates, minEstimate, maxEstimate, avgEstimate, isnull (closestEstimate, 0), getutcdate(), getutcdate(), 0x1
	from	#s
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'New entries added', 'Information', getutcdate(), @executionInstanceId)

	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyEstimateStats', 'Stored Procedure completed', 'Information', getutcdate(), @executionInstanceId)
go

create procedure [dbo].DeDuplicatePropertyPredictions (@executionInstanceId nvarchar(450))
as
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeDuplicatePropertyPredictions', 'Stored Procedure started', 'Information', getutcdate(), @executionInstanceId)

	delete		p1
	from		dbo.PropertyPrediction p1, (
		select		minerId, propertyId, max(id) as id 
		from		dbo.PropertyPrediction 
		where		active = 0x1
		group by	minerId,propertyId 
		having		count(1) > 1) as p2
	where		p1.minerId = p2.minerId
	and			p1.propertyId = p2.propertyId
	and			p1.id < p2.id
	and			p1.active = 0x1

	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeDuplicatePropertyPredictions', 'Stored Procedure completed', 'Information', getutcdate(), @executionInstanceId)
go

create procedure [dbo].[CalculatePropertyPredictionStats] (@executionInstanceId nvarchar(450))
as
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Stored Procedure started', 'Information', getutcdate(), @executionInstanceId)
	
	CREATE TABLE #t1 (
		propertyId bigint,
		propertyPredictionId bigint,
		minerId bigint,
		saleDate datetime2,
		salePrice float,
		predictedSaleDate datetime2,
		predictedSalePrice float,
		predictionDate datetime2);
				 
	DECLARE @today NVARCHAR(10) = CONVERT(NVARCHAR(10), GETUTCDATE(), 120);
	DECLARE @yesterday NVARCHAR(10) = CONVERT(NVARCHAR(10), DATEADD(DAY, -1, GETUTCDATE()), 120);
	DECLARE @dayBeforeYesterday NVARCHAR(10) = CONVERT(NVARCHAR(10), DATEADD(DAY, -2, GETUTCDATE()), 120);
	 
	DECLARE @sql NVARCHAR(MAX) = '
	INSERT INTO #t1
	SELECT 
		p.id AS propertyId,
		pp.id AS propertyPredictionId,
		pp.minerId,
		p.saleDate,
		p.salePrice,
		pp.predictedSaleDate,
		pp.predictedSalePrice,
		pp.predictionDate
	FROM dbo.Property p WITH (NOLOCK)
	INNER JOIN (
		SELECT * FROM dbo.[PropertyPrediction' + @today + ']
		UNION ALL
		SELECT * FROM dbo.[PropertyPrediction' + @yesterday + ']
	) pp ON pp.propertyId = p.id
	WHERE p.active = 1
	AND (
		p.lastUpdateDate > DATEADD(DAY, -1, GETUTCDATE())
		OR pp.predictionDate >= DATEADD(DAY, -1, GETUTCDATE())
	);';

	EXEC sp_executesql @sql;

	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Predictions selected', 'Information', getutcdate(), @executionInstanceId);

	with rankedPredictions as (
		select	p.propertyId, p.saleDate, p.salePrice, p.predictedSaleDate, p.predictedSalePrice, p.predictionDate, m.id as minerId, 
		row_number() over (
			partition by p.propertyId
			order by
				case when p.salePrice is not null then abs(p.predictedSalePrice - p.salePrice) else null end asc,
				case when p.salePrice is null then m.incentive else null end desc) as ranking
		from #t1 p, dbo.Miner m (nolock) where p.minerId = m.id)	
	select		*
	into		#t2
	from		rankedPredictions
	where		ranking <= 10
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Ranked predictions selected', 'Information', getutcdate(), @executionInstanceId)
		
	select		propertyId, 
				count(1) as numPredictions, 
				avg(predictedSalePrice) as avgPredictedSalePrice,
				min(predictedSalePrice) as minPredictedSalePrice,
				max(predictedSalePrice) as maxPredictedSalePrice
	into		#t3
	from		#t1 
	group by	propertyId
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Stats calculated', 'Information', getutcdate(), @executionInstanceId)
		  
	select		propertyId,
				json_query ((
					select	hotKey, coldKey, 
							cast (predictedSalePrice as decimal(18, 2)) as predictedSalePrice, 
							predictedSaleDate,
							predictionDate
					from	#t2 as sub, dbo.Miner m
					where	sub.propertyId = t.propertyId
					and		sub.minerId = m.id
					for json path)) as top10Predictions
	into		#t4
	from (select distinct propertyId from #t2) t;

	alter table	#t3 add top10Predictions nvarchar(max)

	update		p
	set			p.top10Predictions = t.top10Predictions
	from		#t3 p, #t4 t
	where		p.propertyId = t.propertyId
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Ranked predictions json calculated', 'Information', getutcdate(), @executionInstanceId)
	
	delete		s
	from		dbo.PropertyPredictionStats s
	where		s.propertyId in (select propertyId from #t3)
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Old entries deleted', 'Information', getutcdate(), @executionInstanceId)

	insert		dbo.PropertyPredictionStats (propertyId, numPredictions, avgPredictedSalePrice, minPredictedSalePrice, maxPredictedSalePrice, top10Predictions, createDate, lastUpdateDate, active)
	select		propertyId, numPredictions, avgPredictedSalePrice, minPredictedSalePrice, maxPredictedSalePrice, top10Predictions, getutcdate(), getutcdate(), 0x1
	from		#t3
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'New entries added', 'Information', getutcdate(), @executionInstanceId)

	DECLARE @dayBeforeYesterdayTable NVARCHAR(128) = '[PropertyPrediction' + @dayBeforeYesterday + ']';
	DECLARE @dropSql NVARCHAR(MAX) = '
	IF OBJECT_ID(''dbo.' + @dayBeforeYesterdayTable + ''', ''U'') IS NOT NULL
	BEGIN
		DROP TABLE dbo.' + @dayBeforeYesterdayTable + ';
	END';

	EXEC sp_executesql @dropSql;

	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Day before yesterday''s table dropped', 'Information', getutcdate(), @executionInstanceId)
	
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('CalculatePropertyPredictionStats', 'Stored Procedure completed', 'Information', getutcdate(), @executionInstanceId)
go
  
create PROCEDURE [dbo].[DeleteOldProperties]  (@executionInstanceId nvarchar(450))
AS
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeleteOldProperties', 'Stored Procedure started', 'Information', getutcdate(), @executionInstanceId)

    DECLARE @RowCount INT;

    WHILE 1 = 1
    BEGIN
        DECLARE @PropertyIdsToDelete TABLE (id BIGINT);

        INSERT INTO @PropertyIdsToDelete (id)
        SELECT TOP (1000) id
        FROM dbo.Property
        WHERE saleDate < DATEADD(dd, -31, getutcdate());

        SET @RowCount = @@ROWCOUNT;

        IF @RowCount = 0 BREAK;
					
		insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
		values		('DeleteOldProperties', 'Deleting batch of ' + cast (@rowCount as nvarchar (450)) + ' properties', 'Information', getutcdate(), @executionInstanceId)

		DELETE FROM dbo.PropertyImage
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);

		DELETE FROM dbo.PropertyEstimate
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);

		DELETE FROM dbo.PropertyPrediction
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);
		
		DELETE FROM dbo.PropertyEstimateStats
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);

		DELETE FROM dbo.PropertyShare
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);

		DELETE FROM dbo.PropertyPredictionStats
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete);

		DELETE FROM dbo.Property
		WHERE id IN (SELECT id FROM @PropertyIdsToDelete);
    end
		
	select		propertyId, max(id) as maxId
	into		#relisted
	from		dbo.Property 
	group by	propertyid 
	having		count(1) > 1

    WHILE 1 = 1
    BEGIN
        DECLARE @RelistedPropertyIdsToDelete TABLE (id BIGINT);

        INSERT INTO @RelistedPropertyIdsToDelete (id)
        SELECT TOP (1000) id
        FROM dbo.Property p, #relisted r
        WHERE  p.propertyId = r.propertyId
		and		p.id != r.maxId

        SET @RowCount = @@ROWCOUNT;

        IF @RowCount = 0 BREAK;
					
		insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
		values		('DeleteOldProperties', 'Deleting batch of ' + cast (@rowCount as nvarchar (450)) + ' relisted properties', 'Information', getutcdate(), @executionInstanceId)

		DELETE FROM dbo.PropertyImage
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);

		DELETE FROM dbo.PropertyEstimate
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);

		DELETE FROM dbo.PropertyPrediction
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);
		
		DELETE FROM dbo.PropertyEstimateStats
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);

		DELETE FROM dbo.PropertyShare
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);

		DELETE FROM dbo.PropertyPredictionStats
		WHERE propertyId IN (SELECT id FROM @RelistedPropertyIdsToDelete);

		DELETE FROM dbo.Property
		WHERE id IN (SELECT id FROM @RelistedPropertyIdsToDelete);
    end

    WHILE 1 = 1
    BEGIN
        DECLARE @PropertyIdsToDelete2 TABLE (id BIGINT);

        INSERT INTO @PropertyIdsToDelete2 (id)
        SELECT TOP (1000) id
        FROM dbo.Property
        WHERE listingDate < DATEADD(dd, -90, getutcdate());

        SET @RowCount = @@ROWCOUNT;

        IF @RowCount = 0 BREAK;
					
		insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
		values		('DeleteOldProperties', 'Deleting batch of ' + cast (@rowCount as nvarchar (450)) + ' properties unsold in 90 days', 'Information', getutcdate(), @executionInstanceId)

		DELETE FROM dbo.PropertyImage
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);

		DELETE FROM dbo.PropertyEstimate
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);

		DELETE FROM dbo.PropertyPrediction
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);
		
		DELETE FROM dbo.PropertyEstimateStats
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);

		DELETE FROM dbo.PropertyShare
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);

		DELETE FROM dbo.PropertyPredictionStats
		WHERE propertyId IN (SELECT id FROM @PropertyIdsToDelete2);

		DELETE FROM dbo.Property
		WHERE id IN (SELECT id FROM @PropertyIdsToDelete2);
    end 
			


	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeleteOldProperties', 'Stored Procedure completed', 'Information', getutcdate(), @executionInstanceId)
go

create PROCEDURE [dbo].DeleteOldMinerStats  (@executionInstanceId nvarchar(450))
AS
	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeleteOldMinerStats', 'Stored Procedure started', 'Information', getutcdate(), @executionInstanceId)
 
	delete		dbo.MinerDatedScore where minerScoreId in (select id from dbo.MinerScore where active=0x0)
	delete		dbo.MinerScore where active = 0x0 and id not in (select minerScoreId from dbo.MinerDatedScore)

	insert		dbo.FunctionLog (functionName, logEntry, entryType, timeStamp, executionInstanceId)
	values		('DeleteOldMinerStats', 'Stored Procedure completed', 'Information', getutcdate(), @executionInstanceId)
go