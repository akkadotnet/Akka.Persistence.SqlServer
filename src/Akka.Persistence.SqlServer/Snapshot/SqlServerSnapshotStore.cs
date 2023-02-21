// -----------------------------------------------------------------------
// <copyright file="SqlServerSnapshotStore.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.Sql.Common.Snapshot;
using Akka.Persistence.SqlServer.Helpers;
using Microsoft.Data.SqlClient;

namespace Akka.Persistence.SqlServer.Snapshot
{
    public class SqlServerSnapshotStore : SqlSnapshotStore
    {
        private readonly bool _useConstantParameterSize;
        protected readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);

        public SqlServerSnapshotStore(Config snapshotConfig) : base(snapshotConfig)
        {
            var config = snapshotConfig.WithFallback(Extension.DefaultSnapshotConfig);

            _useConstantParameterSize = config.GetBoolean("use-constant-parameter-size");

            var connectionTimeoutSeconds = new SqlConnectionStringBuilder(GetConnectionString()).ConnectTimeout;
            var commandTimeout = config.GetTimeSpan("connection-timeout");
            var circuitBreakerTimeout = snapshotConfig.GetTimeSpan(
                "circuit-breaker.call-timeout");
            var totalTimeout = commandTimeout.Add(
                TimeSpan.FromSeconds(connectionTimeoutSeconds));
            if (totalTimeout >=
                circuitBreakerTimeout)
                Log.Warning(
                    "Configured Total of Connection timeout ({0} seconds) and Command timeout ({1} seconds) is greater than or equal to Circuit breaker timeout ({2} seconds). This may cause unintended write failures",
                    connectionTimeoutSeconds, commandTimeout.TotalSeconds,
                    circuitBreakerTimeout.TotalSeconds);
            QueryExecutor = new SqlServerQueryExecutor(new QueryConfiguration(
                    config.GetString("schema-name"),
                    config.GetString("table-name"),
                    "PersistenceId",
                    "SequenceNr",
                    "Snapshot",
                    "Manifest",
                    "Timestamp",
                    "SerializerId",
                    config.GetTimeSpan("connection-timeout"),
                    config.GetString("serializer"),
                    config.GetBoolean("sequential-access")),
                Context.System.Serialization);
        }

        public override ISnapshotQueryExecutor QueryExecutor { get; }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        /// <inheritdoc />
        protected override void PreStart()
        {
            base.PreStart();

            // if constant parameter sizes required, provide column sizes to query executor
            if (_useConstantParameterSize)
                using (var connection = CreateDbConnection(GetConnectionString()))
                {
                    var columnSizes = ColumnSizeLoader.LoadSnapshotColumnSizes(QueryExecutor.Configuration, connection);
                    (QueryExecutor as SqlServerQueryExecutor)?.SetColumnSizes(columnSizes);
                }
        }
    }
}