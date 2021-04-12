// -----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStore.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.SqlServer.Snapshot
{
    public class SqlServerSnapshotStore : SqlSnapshotStore
    {
        protected readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);

        public SqlServerSnapshotStore(Config snapshotConfig) : base(snapshotConfig)
        {
            var config = snapshotConfig.WithFallback(Extension.DefaultSnapshotConfig);
            var connectionTimeoutSeconds =
                new SqlConnectionStringBuilder(
                    config.GetString("connection-string")).ConnectTimeout;
            var commandTimeout = config.GetTimeSpan("connection-timeout", null);
            var circuitBreakerTimeout = snapshotConfig.GetTimeSpan(
                "circuit-breaker.call-timeout",
                null);
            var totalTimeout = commandTimeout.Add(
                TimeSpan.FromSeconds(connectionTimeoutSeconds));
            if (totalTimeout >=
                circuitBreakerTimeout)
            {
                Log.Warning(
                    "Configured Total of Connection timeout ({0} seconds) and Command timeout ({1} seconds) is greater than or equal to Circuit breaker timeout ({2} seconds). This may cause unintended write failures",
                    connectionTimeoutSeconds, commandTimeout.TotalSeconds,
                    circuitBreakerTimeout.TotalSeconds);
            }
            QueryExecutor = new SqlServerQueryExecutor(new QueryConfiguration(
 
                schemaName: config.GetString("schema-name", null),
                snapshotTableName: config.GetString("table-name", null),
                persistenceIdColumnName: "PersistenceId",
                sequenceNrColumnName: "SequenceNr",
                payloadColumnName: "Snapshot",
                manifestColumnName: "Manifest",
                timestampColumnName: "Timestamp",
                serializerIdColumnName: "SerializerId",
                timeout: config.GetTimeSpan("connection-timeout", null),
                defaultSerializer: config.GetString("serializer", null),
                useSequentialAccess: config.GetBoolean("sequential-access", false)),
                Context.System.Serialization);
        }

        public override ISnapshotQueryExecutor QueryExecutor { get; }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}