BEGIN TRANSACTION;
CREATE TABLE [TripLocations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_TripLocations] PRIMARY KEY ([Id])
);

CREATE TABLE [Trips] (
    [Id] int NOT NULL IDENTITY,
    [TripLocationId] int NOT NULL,
    [TripDate] datetime2 NOT NULL,
    [AdultTicketPrice] decimal(18,2) NOT NULL,
    [ChildTicketPrice] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_Trips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trips_TripLocations_TripLocationId] FOREIGN KEY ([TripLocationId]) REFERENCES [TripLocations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TripBookings] (
    [Id] int NOT NULL IDENTITY,
    [TripId] int NOT NULL,
    [EmployeeNumber] nvarchar(100) NOT NULL,
    [EmployeeName] nvarchar(100) NOT NULL,
    [Sector] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [AdultsCount] int NOT NULL,
    [ChildrenCount] int NOT NULL,
    [AdultUnitPrice] decimal(18,2) NOT NULL,
    [ChildUnitPrice] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [CaseSystemId] nvarchar(max) NOT NULL,
    [WorkflowId] bigint NULL,
    [DocumentId] bigint NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_TripBookings] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TripBookings_AdultsCount_Min] CHECK ([AdultsCount] >= 1),
    CONSTRAINT [CK_TripBookings_ChildrenCount_NonNegative] CHECK ([ChildrenCount] >= 0),
    CONSTRAINT [CK_TripBookings_TotalGuests_Max] CHECK ([AdultsCount] + [ChildrenCount] <= 5),
    CONSTRAINT [FK_TripBookings_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_TripBookings_EmployeeNumber_TripId] ON [TripBookings] ([EmployeeNumber], [TripId]);

CREATE INDEX [IX_TripBookings_TripId] ON [TripBookings] ([TripId]);

CREATE UNIQUE INDEX [IX_TripLocations_Name] ON [TripLocations] ([Name]);

CREATE INDEX [IX_Trips_TripLocationId_TripDate] ON [Trips] ([TripLocationId], [TripDate]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260705121618_AddOneDayTripsModule', N'9.0.8');

ALTER TABLE [TripBookings] DROP CONSTRAINT [CK_TripBookings_TotalGuests_Max];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trips]') AND [c].[name] = N'Status');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Trips] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Trips] DROP COLUMN [Status];

ALTER TABLE [Trips] ADD [CompanionTicketPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TripLocations] ADD [TicketCount] int NOT NULL DEFAULT 0;

ALTER TABLE [TripBookings] ADD [CompanionUnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TripBookings] ADD [CompanionsCount] int NOT NULL DEFAULT 0;

ALTER TABLE [TripBookings] ADD [PaymentDeadline] datetime2 NULL;

ALTER TABLE [TripBookings] ADD CONSTRAINT [CK_TripBookings_CompanionsCount_NonNegative] CHECK ([CompanionsCount] >= 0);

ALTER TABLE [TripBookings] ADD CONSTRAINT [CK_TripBookings_TotalGuests_Max] CHECK ([AdultsCount] + [ChildrenCount] + [CompanionsCount] <= 5);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260707075447_AddTripTicketsCompanionAndPaymentHold', N'9.0.8');

ALTER TABLE [TripBookings] ADD [BookingType] int NOT NULL DEFAULT 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260712131724_AddTripBookingType', N'9.0.8');

CREATE TABLE [HotelCities] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_HotelCities] PRIMARY KEY ([Id])
);

CREATE TABLE [Hotels] (
    [Id] int NOT NULL IDENTITY,
    [HotelCityId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [AdultTicketPrice] decimal(18,2) NOT NULL,
    [ChildTicketPrice] decimal(18,2) NOT NULL,
    [CompanionTicketPrice] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_Hotels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Hotels_HotelCities_HotelCityId] FOREIGN KEY ([HotelCityId]) REFERENCES [HotelCities] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HotelTrips] (
    [Id] int NOT NULL IDENTITY,
    [HotelId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_HotelTrips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HotelTrips_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [Hotels] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Tickets] (
    [Id] int NOT NULL IDENTITY,
    [HotelId] int NOT NULL,
    [Quantity] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tickets_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [Hotels] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [HotelTripBookings] (
    [Id] int NOT NULL IDENTITY,
    [HotelTripId] int NOT NULL,
    [EmployeeNumber] nvarchar(100) NOT NULL,
    [EmployeeName] nvarchar(100) NOT NULL,
    [Sector] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [AdultsCount] int NOT NULL,
    [ChildrenCount] int NOT NULL,
    [CompanionsCount] int NOT NULL,
    [AdultUnitPrice] decimal(18,2) NOT NULL,
    [ChildUnitPrice] decimal(18,2) NOT NULL,
    [CompanionUnitPrice] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [BookingType] int NOT NULL,
    [Status] int NOT NULL,
    [PaymentDeadline] datetime2 NULL,
    [CaseSystemId] nvarchar(max) NOT NULL,
    [WorkflowId] bigint NULL,
    [DocumentId] bigint NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedByUserId] bigint NULL,
    [UpdatedByUserId] bigint NULL,
    CONSTRAINT [PK_HotelTripBookings] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HotelTripBookings_AdultsCount_Min] CHECK ([AdultsCount] >= 1),
    CONSTRAINT [CK_HotelTripBookings_ChildrenCount_NonNegative] CHECK ([ChildrenCount] >= 0),
    CONSTRAINT [CK_HotelTripBookings_CompanionsCount_NonNegative] CHECK ([CompanionsCount] >= 0),
    CONSTRAINT [CK_HotelTripBookings_TotalGuests_Max] CHECK ([AdultsCount] + [ChildrenCount] + [CompanionsCount] <= 5),
    CONSTRAINT [FK_HotelTripBookings_HotelTrips_HotelTripId] FOREIGN KEY ([HotelTripId]) REFERENCES [HotelTrips] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_HotelCities_Name] ON [HotelCities] ([Name]);

CREATE INDEX [IX_Hotels_HotelCityId] ON [Hotels] ([HotelCityId]);

CREATE INDEX [IX_HotelTripBookings_EmployeeNumber_HotelTripId] ON [HotelTripBookings] ([EmployeeNumber], [HotelTripId]);

CREATE INDEX [IX_HotelTripBookings_HotelTripId] ON [HotelTripBookings] ([HotelTripId]);

CREATE INDEX [IX_HotelTrips_HotelId_StartDate_EndDate] ON [HotelTrips] ([HotelId], [StartDate], [EndDate]);

CREATE UNIQUE INDEX [IX_Tickets_HotelId] ON [Tickets] ([HotelId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260713102933_AddHotelTripsModule', N'9.0.8');

EXEC sp_rename N'[TripLocations].[TicketCount]', N'EmployeeTicketCount', 'COLUMN';

EXEC sp_rename N'[Tickets].[Quantity]', N'EmployeeQuantity', 'COLUMN';

ALTER TABLE [TripLocations] ADD [PensionTicketCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Tickets] ADD [PensionQuantity] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260714131821_SplitTicketPoolsByBookingType', N'9.0.8');

ALTER TABLE [TripLocations] ADD [AdultTicketPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TripLocations] ADD [ChildTicketPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [TripLocations] ADD [CompanionTicketPrice] decimal(18,2) NOT NULL DEFAULT 0.0;


                UPDATE tl
                SET tl.AdultTicketPrice = t.AdultTicketPrice,
                    tl.ChildTicketPrice = t.ChildTicketPrice,
                    tl.CompanionTicketPrice = t.CompanionTicketPrice
                FROM TripLocations tl
                CROSS APPLY (
                    SELECT TOP 1 AdultTicketPrice, ChildTicketPrice, CompanionTicketPrice
                    FROM Trips
                    WHERE Trips.TripLocationId = tl.Id
                    ORDER BY Trips.Id DESC
                ) t;

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trips]') AND [c].[name] = N'AdultTicketPrice');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Trips] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Trips] DROP COLUMN [AdultTicketPrice];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trips]') AND [c].[name] = N'ChildTicketPrice');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Trips] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Trips] DROP COLUMN [ChildTicketPrice];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trips]') AND [c].[name] = N'CompanionTicketPrice');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Trips] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Trips] DROP COLUMN [CompanionTicketPrice];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260720133847_MoveTripPricesToLocation', N'9.0.8');

COMMIT;
GO

