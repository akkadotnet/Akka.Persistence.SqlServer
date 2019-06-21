﻿// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerJournal.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

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
            config.GetString("schema-name", "dbo"),
            config.GetString("table-name", "EventJournal"),
            config.GetString("metadata-table-name", "Metadata"),
            "PersistenceId",
            "SequenceNr",
            "Payload",
            "Manifest",
            "Timestamp",
            "IsDeleted",
            "Tags",
            "Ordering",
            "SerializerId",
            config.GetTimeSpan("connection-timeout", TimeSpan.FromSeconds(30)),
            config.GetString("serializer"),
            config.GetBoolean("sequential-access")))
        {
        }

        public BatchingSqlServerJournalSetup(string connectionString, int maxConcurrentOperations, int maxBatchSize,
            int maxBufferSize, bool autoInitialize,
            TimeSpan connectionTimeout, IsolationLevel isolationLevel, CircuitBreakerSettings circuitBreakerSettings,
            ReplayFilterSettings replayFilterSettings, QueryConfiguration namingConventions, string defaultSerialzier)
            : base(connectionString, maxConcurrentOperations, maxBatchSize, maxBufferSize, autoInitialize,
                connectionTimeout, isolationLevel, circuitBreakerSettings, replayFilterSettings, namingConventions,
                defaultSerialzier)
        {
        }
    }

    public class BatchingSqlServerJournal : BatchingSqlJournal<SqlConnection, SqlCommand>
    {
        public BatchingSqlServerJournal(Config config) : this(new BatchingSqlServerJournalSetup(config))
        {
            var c = Setup.NamingConventions;

            ByTagSql = ByTagSql = $@"
             SELECT TOP (@Take)
             e.{c.PersistenceIdColumnName} as PersistenceId, 
             e.{c.SequenceNrColumnName} as SequenceNr, 
             e.{c.TimestampColumnName} as Timestamp, 
             e.{c.IsDeletedColumnName} as IsDeleted, 
             e.{c.ManifestColumnName} as Manifest, 
             e.{c.PayloadColumnName} as Payload,
             e.{c.SerializerIdColumnName} as SerializerId,
             e.{c.OrderingColumnName} as Ordering
             FROM {c.FullJournalTableName} e
             WHERE e.{c.OrderingColumnName} > @Ordering AND e.{c.TagsColumnName} LIKE @Tag
             ORDER BY {c.OrderingColumnName} ASC
             ";
        }

        public BatchingSqlServerJournal(BatchingSqlServerJournalSetup setup) : base(setup)
        {
            var c = Setup.NamingConventions;
            Initializers = ImmutableDictionary.CreateRange(new Dictionary<string, string>
            {
                ["CreateJournalSql"] =
                    $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                            c.SchemaName
                        }' AND TABLE_NAME = '{c.JournalEventsTableName}')
                BEGIN
                    CREATE TABLE {c.FullJournalTableName} (
                        {c.OrderingColumnName} BIGINT IDENTITY(1,1) NOT NULL,
	                    {c.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                    {c.SequenceNrColumnName} BIGINT NOT NULL,
                        {c.TimestampColumnName} BIGINT NOT NULL,
                        {c.IsDeletedColumnName} BIT NOT NULL,
                        {c.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                    {c.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                        {c.TagsColumnName} NVARCHAR(100) NULL,
                        {c.SerializerIdColumnName} INTEGER NULL,
                        CONSTRAINT PK_{c.JournalEventsTableName} PRIMARY KEY ({c.OrderingColumnName}),
                        CONSTRAINT UQ_{c.JournalEventsTableName} UNIQUE ({c.PersistenceIdColumnName}, {
                            c.SequenceNrColumnName
                        })
                    );
                    CREATE INDEX IX_{c.JournalEventsTableName}_{c.SequenceNrColumnName} ON {c.FullJournalTableName}({
                            c.SequenceNrColumnName
                        });
                    CREATE INDEX IX_{c.JournalEventsTableName}_{c.TimestampColumnName} ON {c.FullJournalTableName}({
                            c.TimestampColumnName
                        });
                END",
                ["CreateMetadataSql"] = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                        c.SchemaName
                    }' AND TABLE_NAME = '{c.MetaTableName}')
                BEGIN
                    CREATE TABLE {c.FullMetaTableName} (
	                    {c.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                    {c.SequenceNrColumnName} BIGINT NOT NULL,
                        CONSTRAINT PK_{c.MetaTableName} PRIMARY KEY ({c.PersistenceIdColumnName}, {
                        c.SequenceNrColumnName
                    })
                    );
                END"
            });
        }

        protected override string ByTagSql { get; }
        protected override ImmutableDictionary<string, string> Initializers { get; }

        protected override SqlConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}