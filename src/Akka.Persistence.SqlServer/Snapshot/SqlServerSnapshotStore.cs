// -----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStore.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

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
                    config.GetString("schema-name"),
                    config.GetString("table-name"),
                    "PersistenceId",
                    "SequenceNr",
                    "Snapshot",
                    "Manifest",
                    "Timestamp",
                    "SerializerId",
                    sqlConfig.GetTimeSpan("connection-timeout"),
                    config.GetString("serializer"),
                    config.GetBoolean("sequential-access")),
                Context.System.Serialization);
        }

        public override ISnapshotQueryExecutor QueryExecutor { get; }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}