using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SqlClient;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Journal
{
    public sealed class BatchingSqlServerJournalSetup : BatchingSqlJournalSetup
    {
        public BatchingSqlServerJournalSetup(Config config) : base(config, new QueryConfiguration(
                    schemaName: config.GetString("schema-name", "dbo"),
                    journalEventsTableName: config.GetString("table-name", "EventJournal"),
                    metaTableName: config.GetString("metadata-table-name", "Metadata"),
                    persistenceIdColumnName: "PersistenceId",
                    sequenceNrColumnName: "SequenceNr",
                    payloadColumnName: "Payload",
                    manifestColumnName: "Manifest",
                    timestampColumnName: "Timestamp",
                    isDeletedColumnName: "IsDeleted",
                    tagsColumnName: "Tags",
                    orderingColumnName: "Ordering",
                    timeout: config.GetTimeSpan("connection-timeout", TimeSpan.FromSeconds(30))))
        {
        }

        public BatchingSqlServerJournalSetup(string connectionString, int maxConcurrentOperations, int maxBatchSize, int maxBufferSize, bool autoInitialize,
            TimeSpan connectionTimeout, IsolationLevel isolationLevel, CircuitBreakerSettings circuitBreakerSettings, ReplayFilterSettings replayFilterSettings, QueryConfiguration namingConventions)
            : base(connectionString, maxConcurrentOperations, maxBatchSize, maxBufferSize, autoInitialize, connectionTimeout, isolationLevel, circuitBreakerSettings, replayFilterSettings, namingConventions)
        {
        }
    }

    public class BatchingSqlServerJournal : BatchingSqlJournal<SqlConnection, SqlCommand>
    {
        public BatchingSqlServerJournal(Config config) : this(new BatchingSqlServerJournalSetup(config))
        {
        }

        public BatchingSqlServerJournal(BatchingSqlServerJournalSetup setup) : base(setup)
        {
            var conventions = Setup.NamingConventions;
            Initializers = ImmutableDictionary.CreateRange(new []
            {
                new KeyValuePair<string, string>("CreateJournalSql", $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{conventions.SchemaName}' AND TABLE_NAME = '{conventions.JournalEventsTableName}')
                BEGIN
                    CREATE TABLE {conventions.FullJournalTableName} (
                        {conventions.OrderingColumnName} BIGINT IDENTITY(1,1) PRIMARY KEY NOT NULL,
	                    {conventions.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                    {conventions.SequenceNrColumnName} BIGINT NOT NULL,
                        {conventions.TimestampColumnName} BIGINT NOT NULL,
                        {conventions.IsDeletedColumnName} BIT NOT NULL,
                        {conventions.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                    {conventions.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                        {conventions.TagsColumnName} NVARCHAR(100) NULL,
                        CONSTRAINT UQ_{conventions.JournalEventsTableName} UNIQUE ({conventions.PersistenceIdColumnName}, {conventions.SequenceNrColumnName})
                    );
                    CREATE INDEX IX_{conventions.JournalEventsTableName}_{conventions.SequenceNrColumnName} ON {conventions.FullJournalTableName}({conventions.SequenceNrColumnName});
                    CREATE INDEX IX_{conventions.JournalEventsTableName}_{conventions.TimestampColumnName} ON {conventions.FullJournalTableName}({conventions.TimestampColumnName});
                END"), 
                new KeyValuePair<string, string>("CreateMetadataSql", $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{conventions.SchemaName}' AND TABLE_NAME = '{conventions.MetaTableName}')
                BEGIN
                    CREATE TABLE {conventions.FullMetaTableName} (
	                    {conventions.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                    {conventions.SequenceNrColumnName} BIGINT NOT NULL,
                        CONSTRAINT PK_{conventions.MetaTableName} PRIMARY KEY ({conventions.PersistenceIdColumnName}, {conventions.SequenceNrColumnName})
                    );
                END"), 
            });
        }

        protected override SqlConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
        protected override ImmutableDictionary<string, string> Initializers { get; }
    }
}
