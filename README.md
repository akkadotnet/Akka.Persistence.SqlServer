## Akka.Persistence.SqlServer

Akka Persistence journal and snapshot store backed by SQL Server database.

**WARNING: Akka.Persistence.SqlServer plugin is still in beta and it's mechanics described bellow may be still subject to change**.

### Configuration

Both journal and snapshot store share the same configuration keys (however they resides in separate scopes, so they are definied distinctly for either journal or snapshot store):

Remember that connection string must be provided separately to Journal and Snapshot Store.

```hocon
akka.persistence{

	journal {
		plugin = "akka.persistence.journal.sql-server"
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
		plugin = "akka.persistence.snapshot-store.sql-server"
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
### Table Schema

SQL Server persistence plugin defines a default table schema used for journal, snapshot store and metadate table.

```SQL
CREATE TABLE {your_journal_table_name} (
  PersistenceID NVARCHAR(255) NOT NULL,
  SequenceNr BIGINT NOT NULL,
  Timestamp DATETIME2 NOT NULL,
  IsDeleted BIT NOT NULL,
  Manifest NVARCHAR(500) NOT NULL,
  Payload VARBINARY(MAX) NOT NULL
  CONSTRAINT PK_{your_journal_table_name} PRIMARY KEY (PersistenceID, SequenceNr)
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

#### From 1.0.6
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
