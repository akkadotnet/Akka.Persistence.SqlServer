#### 1.4.0 June 21 2019 ####
* First beta of Akka.Persistence.SqlServer 1.4.0 - should resolve a number of issues with Akka.Cluster.Sharding recovery and BatchingSqlJournal deadlocks.

#### 1.3.13 April 30 2019 ####
* Upgrades to Akka.Persistence v1.3.13
* Resolves https://github.com/akkadotnet/Akka.Persistence.SqlServer/issues/104 - major issue with BatchingSqlJournal.

#### 1.3.7 June 04 2018 ####
Upgrades to Akka.Persistence 1.3.7, which includes some major changes for SQL-based SnapShot stores, which you can read more about here: https://github.com/akkadotnet/akka.net/issues/3422

This should significantly improve the performance of SQL-based journals for loading large snapshots.

Also, removes the dependency on the Akka.TestKit for this package.


#### 1.3.2 October 29 2017 ####

Updated to Akka.Persistence v1.3.2. Fixed bug in SnapshotStore query.

#### 1.3.1 September 11 2017 ####

Support for Akka.NET 1.3, .NET Standard 1.6, and the first stable RTM release of Akka.Persistence.

**Migration from 1.1.1.7-beta Up**

The event journal and snapshot store schema has changed with this release.  In order to keep existing stores compatible with this release, you **must** add a column to both stores for `SerializerId` like so:

```sql
ALTER TABLE {your_journal_table_name} ADD COLUMN SerializerId INTEGER NULL
ALTER TABLE {your_snapshot_table_name} ADD COLUMN SerializerId INTEGER NULL
```

#### 1.1.1 August 01 2016 ####
Support for Akka.NET 1.1 and Akka.Persistence.Query

#### 1.0.8 May 31 2016 ####
Support for Akka 1.0.7
 
#### 1.0.7 April 08 2016 ####
Support for Akka 1.0.7
 
#### 1.0.6 January 22 2016 ####
Upgraded core Akka.Persistence dependencies up to v1.0.6.

Made additional schema changes to underlying SQL server table definition. Migration guide:

**Migration from 1.0.4 up**

The number of schema changes occurred between versions 1.0.4 and 1.0.5, including:

- EventJournal table got Timestamp column (used only for querying).
- EventJournal table dropped CS_PID column - primary key now relies on PersistenceID and SequenceNr directly.
- EventJournal and SnapshotStore tables have PayloadType column renamed to Manifest.

In case of the problems you may migrate your existing database columns using following script:

```sql
-- use default GETDATE in case when you have existing events inside the journal
ALTER TABLE dbo.EventJournal ADD Timestamp DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE dbo.EventJournal DROP CONSTRAINT PK_EventJournal;
ALTER TABLE dbo.EventJournal DROP COLUMN CS_PID;
ALTER TABLE dbo.EventJournal ADD CONSTRAINT PK_EventJournal PRIMARY KEY (PersistenceID, SequenceNr);
sp_RENAME 'EventJournal.PayloadType', 'Manifest', 'COLUMN';

sp_RENAME 'SnapshotStore.PayloadType', 'Manifest', 'COLUMN';
```


#### 1.0.5 August 08 2015 ####

- Changed tables schema: removed CS_PID column from journal and snapshot tables
- Changed tables schema: renamed PayloadType column to Manifest for journal and snapshot tables
- Changed tables schema: added Timestamp column to journal table
- Added compatibility with Persistent queries API
- Added ability to specify connection string stored in \*.config files

#### 1.0.4 August 07 2015 ####

#### 1.0.3 June 12 2015 ####
**Bugfix release for Akka.NET v1.0.2.**

This release addresses an issue with Akka.Persistence.SqlServer and Akka.Persistence.PostgreSql where both packages were missing a reference to Akka.Persistence.Sql.Common.

In Akka.NET v1.0.3 we've packaged Akka.Persistence.Sql.Common into its own NuGet package and referenced it in the affected packages.

#### 1.0.2 June 2 2015
Initial Release of Akka.Persistence.PostgreSql

Fixes & Changes - Akka.Persistence
* [Renamed GuaranteedDelivery classes to AtLeastOnceDelivery](https://github.com/akkadotnet/akka.net/pull/984)
* [Changes in Akka.Persistence SQL backend](https://github.com/akkadotnet/akka.net/pull/963)
* [PostgreSQL persistence plugin for both event journal and snapshot store](https://github.com/akkadotnet/akka.net/pull/971)
* [Cassandra persistence plugin](https://github.com/akkadotnet/akka.net/pull/995)

**New Features:**

**Akka.Persistence.PostgreSql** and **Akka.Persistence.Cassandra**
Akka.Persistence now has two additional concrete implementations for PostgreSQL and Cassandra! You can install either of the packages using the following commandline:

[Akka.Persistence.PostgreSql Configuration Docs](https://github.com/akkadotnet/akka.net/tree/dev/src/contrib/persistence/Akka.Persistence.PostgreSql)
```
PM> Install-Package Akka.Persistence.PostgreSql
```
