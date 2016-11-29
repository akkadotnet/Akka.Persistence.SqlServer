using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Journal
{
    public sealed class BatchingSqlServerJournalSetup : BatchingSqlJournalSetup
    {
        public static BatchingSqlServerJournalSetup Create(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), "Sql journal settings cannot be initialized, because required HOCON section couldn't been found");

            var connectionString = config.GetString("connection-string");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = ConfigurationManager
                    .ConnectionStrings[config.GetString("connection-string-name", "DefaultConnection")]
                    .ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("No connection string for Sql Event Journal was specified");

            return new BatchingSqlServerJournalSetup(
                connectionString: connectionString,
                maxConcurrentOperations: config.GetInt("max-concurrent-operations", 64),
                maxBatchSize: config.GetInt("max-batch-size", 100),
                autoInitialize: config.GetBoolean("auto-initialize", false),
                connectionTimeout: config.GetTimeSpan("connection-timeout", TimeSpan.FromSeconds(30)),
                circuitBreakerSettings: CircuitBreakerSettings.Create(config.GetConfig("circuit-breaker")),
                namingConventions: new QueryConfiguration(
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
                    timeout: config.GetTimeSpan("connection-timeout", TimeSpan.FromSeconds(30))));
        }

        public BatchingSqlServerJournalSetup(string connectionString, int maxConcurrentOperations, int maxBatchSize, bool autoInitialize,
            TimeSpan connectionTimeout, CircuitBreakerSettings circuitBreakerSettings, QueryConfiguration namingConventions)
            : base(connectionString, maxConcurrentOperations, maxBatchSize, autoInitialize, connectionTimeout, circuitBreakerSettings, namingConventions)
        {
        }
    }

    public class BatchingSqlServerJournal : BatchingSqlJournal<BatchingSqlServerJournalSetup>
    {
        public BatchingSqlServerJournal(Config config) : this(BatchingSqlServerJournalSetup.Create(config))
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

        protected override DbConnection CreateConnection() => new SqlConnection(Setup.ConnectionString);
        protected override ImmutableDictionary<string, string> Initializers { get; }
    }
}
