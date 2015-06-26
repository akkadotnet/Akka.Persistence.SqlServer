## Akka.Persistence.SqlServer

Akka Persistence journal and snapshot store backed by SQL Server database.

**WARNING: Akka.Persistence.SqlServer plugin is still in beta and it's mechanics described bellow may be still subject to change**.

### Setup

To activate the journal plugin, add the following lines to actor system configuration file:

```
akka.persistence.journal.plugin = "akka.persistence.journal.sql-server"
akka.persistence.journal.sql-server.connection-string = "<database connection string>"
```

Similar configuration may be used to setup a SQL Server snapshot store:

```
akka.persistence.snapshot-store.plugin = "akka.persistence.snapshot-store.sql-server"
akka.persistence.snapshot-store.sql-server.connection-string = "<database connection string>"
```

Remember that connection string must be provided separately to Journal and Snapshot Store. To finish setup simply initialize plugin using: `SqlServerPersistence.Init(actorSystem);`

### Configuration

Both journal and snapshot store share the same configuration keys (however they resides in separate scopes, so they are definied distinctly for either journal or snapshot store):

- `class` (string with fully qualified type name) - determines class to be used as a persistent journal. Default: *Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer* (for journal) and *Akka.Persistence.SqlServer.Snapshot.SqlServerSnapshotStore, Akka.Persistence.SqlServer* (for snapshot store).
- `plugin-dispatcher` (string with configuration path) - describes a message dispatcher for persistent journal. Default: *akka.actor.default-dispatcher*
- `connection-string` - connection string used to access SQL Server database. Default: *none*.
- `connection-timeout` - timespan determining default connection timeouts on database-related operations. Default: *30s*
- `schema-name` - name of the database schema, where journal or snapshot store tables should be placed. Default: *dbo*
- `table-name` - name of the table used by either journal or snapshot store. Default: *EventJournal* (for journal) or *SnapshotStore* (for snapshot store)
- `auto-initialize` - flag determining if journal or snapshot store related tables should by automatically created when they have not been found in connected database. Default: *false*

### Custom SQL data queries

SQL Server persistence plugin defines a default table schema used for both journal and snapshot store.

**EventJournal table**:

    +---------------+--------+------------+-----------+---------------+----------------+
    | PersistenceId | CS_PID | SequenceNr | IsDeleted |  PayloadType  |     Payload    |
    +---------------+--------+------------+-----------+---------------+----------------+
    | nvarchar(200) |  int   |   bigint   |    bit    | nvarchar(500) | varbinary(max) |
    +---------------+--------+------------+-----------+---------------+----------------+
 
**SnapshotStore table**:
 
    +---------------+--------+------------+-----------+-----------+---------------+-----------------+
    | PersistenceId | CS_PID | SequenceNr | Timestamp | IsDeleted | SnapshotType  |     Snapshot    |
    +---------------+--------+------------+-----------+-----------+---------------+-----------------+
    | nvarchar(200) |  int   |   bigint   | datetime2 |    bit    | nvarchar(500) |  varbinary(max) |
    +---------------+--------+------------+-----------+-----------+---------------+-----------------+

While most of the tables columns maps directly to persistence primitives and are required, CS_PID cached a PersistenceId checksum and server only for performance.

Underneath Akka.Persistence.SqlServer uses a raw ADO.NET commands. You may choose not to use a dedicated built in ones, but to create your own being better fit for your use case. To do so, you have to create your own versions of `IJournalQueryBuilder` and `IJournalQueryMapper` (for custom journals) or `ISnapshotQueryBuilder` and `ISnapshotQueryMapper` (for custom snapshot store) and then attach inside journal, just like in the example below:

```csharp
class MyCustomSqlServerJournal: Akka.Persistence.SqlServer.Journal.SqlServerJournal 
{
    public MyCustomSqlServerJournal() : base() 
    {
        QueryBuilder = new MyCustomJournalQueryBuilder();
        QueryMapper = new MyCustomJournalQueryMapper();
    }
}
```

The final step is to setup your custom journal using akka config:

```
akka.persistence.journal.sql-server.class = "MyModule.MyCustomSqlServerJournal, MyModule"
```
### Tests

The SqlServer tests are packaged and run as part of the default "All" build task.

In order to run the tests, you must do the following things:

1. Download and install SQL Server Express 2014 from: http://www.microsoft.com/en-us/server-cloud/Products/sql-server-editions/sql-server-express.aspx
2. Install SQL Server Express with the default settings.  The default connection string uses the following credentials: `Data Source=localhost\SQLEXPRESS;Integrated Security=True`
3. A custom app.config file can be used and needs to be placed in the same folder as the dll