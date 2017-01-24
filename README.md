## Akka.Persistence.SqlServer

Akka Persistence journal and snapshot store backed by SQL Server database.

**WARNING: Akka.Persistence.SqlServer plugin is still in beta and it's mechanics described bellow may be still subject to change**.

### Configuration

Both journal and snapshot store share the same configuration keys (however they resides in separate scopes, so they are definied distinctly for either journal or snapshot store):

Remember that connection string must be provided separately to Journal and Snapshot Store.

```hocon
akka.persistence{
	journal {
		sql-server {
			# qualified type name of the SQL Server persistence journal actor
			class = "Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer"

			# dispatcher used to drive journal actor
			plugin-dispatcher = "akka.actor.default-dispatcher"

			# connection string used for database access
			connection-string = ""

			# default SQL commands timeout
			connection-timeout = 30s

			# SQL server schema name to table corresponding with persistent journal
			schema-name = dbo

			# SQL server table corresponding with persistent journal
			table-name = EventJournal

			# should corresponding journal table be initialized automatically
			auto-initialize = off
			
			# timestamp provider used for generation of journal entries timestamps
			timestamp-provider = "Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common"

			# metadata table
			metadata-table-name = Metadata
		}
	}

	snapshot-store {
		sql-server {
		
			# qualified type name of the SQL Server persistence journal actor
			class = "Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer"

			# dispatcher used to drive journal actor
			plugin-dispatcher = ""akka.actor.default-dispatcher""

			# connection string used for database access
			connection-string = ""

			# default SQL commands timeout
			connection-timeout = 30s

			# SQL server schema name to table corresponding with persistent journal
			schema-name = dbo

			# SQL server table corresponding with persistent journal
			table-name = SnapshotStore

			# should corresponding journal table be initialized automatically
			auto-initialize = off
		}
	}
}
```

### Batching journal

Since version 1.1.3 an alternative, experimental type of the journal has been released, known as batching journal. It's optimized for concurrent writes made by multiple persistent actors, thanks to the ability of batching multiple SQL operations to be executed within the same database connection. In some of those situations we've noticed over an order of magnitude in event write speed.

To use batching journal, simply change `akka.persistence.journal.sql-server.class` to *Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer*.

Additionally to the existing settings, batching journal introduces few more:

- `isolation-level` to define isolation level for transactions used withing event reads/writes. Possible options: *unspecified* (default), *chaos*, *read-committed*, *read-uncommitted*, *repeatable-read*, *serializable* or *snapshot*.
- `max-concurrent-operations` is used to limit the maximum number of database connections used by this journal. You can use them in situations when you want to partition the same ADO.NET pool between multiple components. Current default: *64*.
- `max-batch-size` defines the maximum number of SQL operations, that are allowed to be executed using the same connection. When there are more operations, they will chunked into subsequent connections. Current default: *100*.
- `max-buffer-size` defines maximum buffer capacity for the requests send to a journal. Once buffer gets overflown, a journal will call `OnBufferOverflow` method. By default it will reject all incoming requests until the buffer space gets freed. You can inherit from `BatchingSqlServerJournal` and override that method to provide a custom backpressure strategy. Current default: *500 000*.

### Table Schema

SQL Server persistence plugin defines a default table schema used for journal, snapshot store and metadata table.

```SQL
CREATE TABLE {your_journal_table_name} (
  Ordering BIGINT IDENTITY(1,1) PRIMARY KEY NOT NULL,
  PersistenceID NVARCHAR(255) NOT NULL,
  SequenceNr BIGINT NOT NULL,
  Timestamp BIGINT NOT NULL,
  IsDeleted BIT NOT NULL,
  Manifest NVARCHAR(500) NOT NULL,
  Payload VARBINARY(MAX) NOT NULL,
  Tags NVARCHAR(100) NULL
  CONSTRAINT QU_{your_journal_table_name} UNIQUE (PersistenceID, SequenceNr)
);

CREATE TABLE {your_snapshot_table_name} (
  PersistenceID NVARCHAR(255) NOT NULL,
  SequenceNr BIGINT NOT NULL,
  Timestamp DATETIME2 NOT NULL,
  Manifest NVARCHAR(500) NOT NULL,
  Snapshot VARBINARY(MAX) NOT NULL
  CONSTRAINT PK_{your_snapshot_table_name} PRIMARY KEY (PersistenceID, SequenceNr)
);

CREATE TABLE {your_metadata_table_name} (
  PersistenceID NVARCHAR(255) NOT NULL,
  SequenceNr BIGINT NOT NULL,
  CONSTRAINT PK_{your_metadata_table_name} PRIMARY KEY (PersistenceID, SequenceNr)
);
```
Underneath Akka.Persistence.SqlServer uses a raw ADO.NET commands. You may choose not to use a dedicated built in ones, but to create your own being better fit for your use case. To do so, you have to create your own versions of `IJournalQueryBuilder` and `IJournalQueryMapper` (for custom journals) or `ISnapshotQueryBuilder` and `ISnapshotQueryMapper` (for custom snapshot store) and then attach inside journal, just like in the example below:

```C#
class MyCustomSqlServerJournal: Akka.Persistence.SqlServer.Journal.SqlServerJournal
{
    public MyCustomSqlServerJournal() : base()
    {
        QueryBuilder = new MyCustomJournalQueryBuilder();
        QueryMapper = new MyCustomJournalQueryMapper();
    }
}
```

### Migration

#### From 1.1.0 to 1.1.2

```sql
ALTER TABLE {your_journal_table_name} DROP CONSTRAINT PK_{your_journal_table_name};
ALTER TABLE {your_journal_table_name} ADD Ordering BIGINT IDENTITY(1,1) PRIMARY KEY NOT NULL;
ALTER TABLE ADD CONSTRAINT QU_{your_journal_table_name} UNIQUE (PersistenceID, SequenceNr);
```

#### From 1.0.8 to 1.1.0
```SQL
-- helper function to convert between DATETIME2 and BIGINT as .NET ticks
-- taken from: http://stackoverflow.com/questions/7386634/convert-sql-server-datetime-object-to-bigint-net-ticks
CREATE FUNCTION [dbo].[Ticks] (@dt DATETIME)
RETURNS BIGINT
WITH SCHEMABINDING
AS
BEGIN 
DECLARE @year INT = DATEPART(yyyy, @dt)
DECLARE @month INT = DATEPART(mm, @dt)
DECLARE @day INT = DATEPART(dd, @dt)
DECLARE @hour INT = DATEPART(hh, @dt)
DECLARE @min INT = DATEPART(mi, @dt)
DECLARE @sec INT = DATEPART(ss, @dt)

DECLARE @days INT =
    CASE @month - 1
        WHEN 0 THEN 0
        WHEN 1 THEN 31
        WHEN 2 THEN 59
        WHEN 3 THEN 90
        WHEN 4 THEN 120
        WHEN 5 THEN 151
        WHEN 6 THEN 181
        WHEN 7 THEN 212
        WHEN 8 THEN 243
        WHEN 9 THEN 273
        WHEN 10 THEN 304
        WHEN 11 THEN 334
        WHEN 12 THEN 365
    END
    IF  @year % 4 = 0 AND (@year % 100  != 0 OR (@year % 100 = 0 AND @year % 400 = 0)) AND @month > 2 BEGIN
        SET @days = @days + 1
    END
RETURN CONVERT(bigint, 
    ((((((((@year - 1) * 365) + ((@year - 1) / 4)) - ((@year - 1) / 100)) + ((@year - 1) / 400)) + @days) + @day) - 1) * 864000000000) +
    ((((@hour * 3600) + CONVERT(bigint, @min) * 60) + CONVERT(bigint, @sec)) * 10000000) + (CONVERT(bigint, DATEPART(ms, @dt)) * CONVERT(bigint,10000));

END;
ALTER TABLE {your_journal_table_name} ADD Timestamp_tmp BIGINT NULL;
UPDATE {your_journal_table_name} SET Timestamp_tmp = dbo.Ticks(Timestamp);
ALTER TABLE {your_journal_table_name} DROP COLUMN Timestamp;
ALTER TABLE {your_journal_table_name} ALTER COLUMN Timestamp_tmp BIGINT NOT NULL;
EXEC sp_RENAME '{your_journal_table_name}.Timestamp_tmp' , 'Timestamp', 'COLUMN';
ALTER TABLE {your_journal_table_name} ADD Tags NVARCHAR(100) NULL;
```

#### From 1.0.6 to 1.0.8
```SQL
CREATE TABLE {your_metadata_table_name} (
  PersistenceID NVARCHAR(255) NOT NULL,
  SequenceNr BIGINT NOT NULL,
  CONSTRAINT PK_Metadata PRIMARY KEY (PersistenceID, SequenceNr)
);

INSERT INTO {your_metadata_table_name} (PersistenceID, SequenceNr)
SELECT PersistenceID, MAX(SequenceNr) as SequenceNr FROM {your_journal_table_name} GROUP BY PersistenceID;

ALTER TABLE {your_journal_table_name} ALTER COLUMN PersistenceID NVARCHAR(255) [NOT NULL];
```

#### From 1.0.4 to 1.0.5
```SQL
ALTER TABLE dbo.EventJournal ADD Timestamp DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE dbo.EventJournal DROP CONSTRAINT PK_EventJournal;
ALTER TABLE dbo.EventJournal DROP COLUMN CS_PID;
ALTER TABLE dbo.EventJournal ADD CONSTRAINT PK_EventJournal PRIMARY KEY (PersistenceID, SequenceNr);
sp_RENAME 'EventJournal.PayloadType', 'Manifest', 'COLUMN';
sp_RENAME 'SnapshotStore.PayloadType', 'Manifest', 'COLUMN';
```

### Tests

The SqlServer tests are packaged and run as part of the default "All" build task.

In order to run the tests, you must do the following things:

1. Download and install SQL Server Express 2014 from: http://www.microsoft.com/en-us/server-cloud/Products/sql-server-editions/sql-server-express.aspx
2. Install SQL Server Express with the default settings.
3. Create a new user called `akkadotnet` with the password `akkadotnet` and give them rights to create new databases on the server.
4. The default connection string uses the following credentials: `Data Source=localhost\SQLEXPRESS;Database=akka_persistence_tests;User Id=akkadotnet;
Password=akkadotnet;`
5. A custom app.config file can be used and needs to be placed in the same folder as the dll
