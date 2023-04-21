// -----------------------------------------------------------------------
// <copyright file="SqlServerJournal.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Akka.Annotations;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.Sql.Common;
using Akka.Persistence.Sql.Common.Journal;
using Akka.Persistence.SqlServer.Helpers;
using Microsoft.Data.SqlClient;

namespace Akka.Persistence.SqlServer.Journal
{
    public class SqlServerJournal : SqlJournal
    {
        public static readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);
        private readonly bool _useConstantParameterSize;

        public SqlServerJournal(Config journalConfig) : base(journalConfig)
        {
            var config = journalConfig.WithFallback(Extension.DefaultJournalConfig);

            _useConstantParameterSize = config.GetBoolean("use-constant-parameter-size");

            var connectionTimeoutSeconds =
                new SqlConnectionStringBuilder(GetConnectionString()).ConnectTimeout;
            var commandTimeout = config.GetTimeSpan("connection-timeout");
            var circuitBreakerTimeout = journalConfig.GetTimeSpan(
                "circuit-breaker.call-timeout");
            var totalTimeout = commandTimeout.Add(
                TimeSpan.FromSeconds(connectionTimeoutSeconds));
            if (totalTimeout >=
                circuitBreakerTimeout)
                Log.Warning(
                    "Configured Total of Connection timeout ({0} seconds) and Command timeout ({1} seconds) is greater than or equal to Circuit breaker timeout ({2} seconds). This may cause unintended write failures",
                    connectionTimeoutSeconds, commandTimeout.TotalSeconds,
                    circuitBreakerTimeout.TotalSeconds);
            QueryExecutor = new SqlServerQueryExecutor(
                CreateQueryConfiguration(config, Settings),
                Context.System.Serialization,
                GetTimestampProvider(config.GetString("timestamp-provider")));
        }

        [InternalApi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static QueryConfiguration CreateQueryConfiguration(Config config, JournalSettings settings)
        {
            return new QueryConfiguration(
                schemaName: config.GetString("schema-name"),
                journalEventsTableName: config.GetString("table-name"),
                metaTableName: config.GetString("metadata-table-name"),
                persistenceIdColumnName: "PersistenceId",
                sequenceNrColumnName: "SequenceNr",
                payloadColumnName: "Payload",
                manifestColumnName: "Manifest",
                timestampColumnName: "Timestamp",
                isDeletedColumnName: "IsDeleted",
                tagsColumnName: "Tags",
                orderingColumnName: "Ordering",
                serializerIdColumnName: "SerializerId",
                timeout: config.GetTimeSpan("connection-timeout"),
                defaultSerializer: config.GetString("serializer"),
                useSequentialAccess: config.GetBoolean("sequential-access"),
                readIsolationLevel: settings.ReadIsolationLevel,
                writeIsolationLevel: settings.WriteIsolationLevel);
        }

        protected override string JournalConfigPath => SqlServerJournalSettings.ConfigPath;
        public override IJournalQueryExecutor QueryExecutor { get; }

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
                    var columnSizes = ColumnSizeLoader.LoadJournalColumnSizes(QueryExecutor.Configuration, connection);
                    (QueryExecutor as SqlServerQueryExecutor)?.SetColumnSizes(columnSizes);
                }
        }
    }
}