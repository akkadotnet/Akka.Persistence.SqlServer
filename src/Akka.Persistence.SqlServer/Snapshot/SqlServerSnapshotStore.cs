//-----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStore.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Data.Common;
using System.Data.SqlClient;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.SqlServer.Snapshot
{
    public class SqlServerSnapshotStore : SqlSnapshotStore
    {
        protected readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);
        public SqlServerSnapshotStore(Config config) : base(config)
        {
            var sqlConfig = config.WithFallback(Extension.DefaultSnapshotConfig);
            QueryExecutor = new SqlServerQueryExecutor(new QueryConfiguration(
                
                schemaName: config.GetString("schema-name"),
                snapshotTableName: config.GetString("table-name"),
                persistenceIdColumnName: "PersistenceId",
                sequenceNrColumnName: "SequenceNr",
                payloadColumnName: "Snapshot",
                manifestColumnName: "Manifest",
                timestampColumnName: "Timestamp",
                timeout: sqlConfig.GetTimeSpan("connection-timeout"),
                defaultSerializer: config.GetString("serializer")),
                
                Context.System.Serialization);
        }

        protected override DbConnection CreateDbConnection(string connectionString) => new SqlConnection(connectionString);

        public override ISnapshotQueryExecutor QueryExecutor { get; }
    }
}