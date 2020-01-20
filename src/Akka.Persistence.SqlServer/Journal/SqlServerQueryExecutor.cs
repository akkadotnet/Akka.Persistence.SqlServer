// -----------------------------------------------------------------------
// <copyright file="SqlServerQueryExecutor.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Data.SqlClient;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Journal
{
    public class SqlServerQueryExecutor : AbstractQueryExecutor
    {
        public SqlServerQueryExecutor(QueryConfiguration configuration, Akka.Serialization.Serialization serialization,
            ITimestampProvider timestampProvider)
            : base(configuration, serialization, timestampProvider)
        {
            ByTagSql = $@"
            DECLARE @Tag_sized NVARCHAR(100);
            SET @Tag_sized = @Tag;
            SELECT TOP (@Take)
            e.{Configuration.PersistenceIdColumnName} as PersistenceId, 
            e.{Configuration.SequenceNrColumnName} as SequenceNr, 
            e.{Configuration.TimestampColumnName} as Timestamp, 
            e.{Configuration.IsDeletedColumnName} as IsDeleted, 
            e.{Configuration.ManifestColumnName} as Manifest, 
            e.{Configuration.PayloadColumnName} as Payload,
            e.{Configuration.SerializerIdColumnName} as SerializerId,
            e.{Configuration.OrderingColumnName} as Ordering
            FROM {Configuration.FullJournalTableName} e
            WHERE e.{Configuration.OrderingColumnName} > @Ordering AND e.{Configuration.TagsColumnName} LIKE @Tag_sized
            ORDER BY {Configuration.OrderingColumnName} ASC
            ";
            CreateEventsJournalSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                    configuration.SchemaName
                }' AND TABLE_NAME = '{configuration.JournalEventsTableName}')
            BEGIN
                CREATE TABLE {configuration.FullJournalTableName} (
                    {configuration.OrderingColumnName} BIGINT IDENTITY(1,1) NOT NULL,
	                {configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    {configuration.TimestampColumnName} BIGINT NOT NULL,
                    {configuration.IsDeletedColumnName} BIT NOT NULL,
                    {configuration.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                {configuration.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                    {configuration.TagsColumnName} NVARCHAR(100) NULL,
                    {configuration.SerializerIdColumnName} INTEGER NULL,
                    CONSTRAINT PK_{configuration.JournalEventsTableName} PRIMARY KEY ({
                    configuration.OrderingColumnName
                }),
                    CONSTRAINT UQ_{configuration.JournalEventsTableName} UNIQUE ({
                    configuration.PersistenceIdColumnName
                }, {configuration.SequenceNrColumnName})
                );
                CREATE INDEX IX_{configuration.JournalEventsTableName}_{configuration.SequenceNrColumnName} ON {
                    configuration.FullJournalTableName
                }({configuration.SequenceNrColumnName});
                CREATE INDEX IX_{configuration.JournalEventsTableName}_{configuration.TimestampColumnName} ON {
                    configuration.FullJournalTableName
                }({configuration.TimestampColumnName});
            END
            ";
            CreateMetaTableSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                    configuration.SchemaName
                }' AND TABLE_NAME = '{configuration.MetaTableName}')
            BEGIN
                CREATE TABLE {configuration.FullMetaTableName} (
	                {configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    CONSTRAINT PK_{configuration.MetaTableName} PRIMARY KEY ({configuration.PersistenceIdColumnName}, {
                    configuration.SequenceNrColumnName
                })
                );
            END
            ";
        }

        protected override string ByTagSql { get; }
        protected override string CreateEventsJournalSql { get; }
        protected override string CreateMetaTableSql { get; }

        protected override DbCommand CreateCommand(DbConnection connection)
        {
            return new SqlCommand {Connection = (SqlConnection) connection};
        }
    }
}