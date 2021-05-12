// -----------------------------------------------------------------------
// <copyright file="SqlServerQueryExecutor.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Akka.Persistence.Sql.Common.Journal;
using Akka.Persistence.SqlServer.Helpers;
using Akka.Util;

namespace Akka.Persistence.SqlServer.Journal
{
    public class SqlServerQueryExecutor : AbstractQueryExecutor
    {
        private Option<JournalColumnSizesInfo> _columnSizes = Option<JournalColumnSizesInfo>.None;

        public SqlServerQueryExecutor(
            QueryConfiguration configuration, 
            Akka.Serialization.Serialization serialization,
            ITimestampProvider timestampProvider)
            : base(configuration, serialization, timestampProvider)
        {
            var allEventColumnNames = $@"
                e.{Configuration.PersistenceIdColumnName} as PersistenceId, 
                e.{Configuration.SequenceNrColumnName} as SequenceNr, 
                e.{Configuration.TimestampColumnName} as Timestamp, 
                e.{Configuration.IsDeletedColumnName} as IsDeleted, 
                e.{Configuration.ManifestColumnName} as Manifest, 
                e.{Configuration.PayloadColumnName} as Payload,
                e.{Configuration.SerializerIdColumnName} as SerializerId";

            ByTagSql = $@"
            DECLARE @Tag_sized NVARCHAR(100);
            SET @Tag_sized = @Tag;
            SELECT TOP (@Take) 
            {allEventColumnNames}, e.{Configuration.OrderingColumnName} as Ordering
            FROM {Configuration.FullJournalTableName} e
            WHERE e.{Configuration.OrderingColumnName} > @Ordering AND e.{Configuration.TagsColumnName} LIKE @Tag_sized
            ORDER BY {Configuration.OrderingColumnName} ASC
            ";

            AllEventsSql = $@"
            SELECT TOP (@Take)
            {allEventColumnNames}, e.{Configuration.OrderingColumnName} as Ordering
            FROM {Configuration.FullJournalTableName} e
            WHERE e.{Configuration.OrderingColumnName} > @Ordering
            ORDER BY {Configuration.OrderingColumnName} ASC";

            CreateEventsJournalSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                    Configuration.SchemaName
                }' AND TABLE_NAME = '{Configuration.JournalEventsTableName}')
            BEGIN
                CREATE TABLE {Configuration.FullJournalTableName} (
                    {Configuration.OrderingColumnName} BIGINT IDENTITY(1,1) NOT NULL,
	                {Configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {Configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    {Configuration.TimestampColumnName} BIGINT NOT NULL,
                    {Configuration.IsDeletedColumnName} BIT NOT NULL,
                    {Configuration.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                {Configuration.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                    {Configuration.TagsColumnName} NVARCHAR(100) NULL,
                    {Configuration.SerializerIdColumnName} INTEGER NULL,
                    CONSTRAINT PK_{Configuration.JournalEventsTableName} PRIMARY KEY ({
                    Configuration.OrderingColumnName
                }),
                    CONSTRAINT UQ_{Configuration.JournalEventsTableName} UNIQUE ({
                    Configuration.PersistenceIdColumnName
                }, {Configuration.SequenceNrColumnName})
                );
                CREATE INDEX IX_{Configuration.JournalEventsTableName}_{Configuration.SequenceNrColumnName} ON {
                    Configuration.FullJournalTableName
                }({Configuration.SequenceNrColumnName});
                CREATE INDEX IX_{Configuration.JournalEventsTableName}_{Configuration.TimestampColumnName} ON {
                    Configuration.FullJournalTableName
                }({Configuration.TimestampColumnName});
            END
            ";
            CreateMetaTableSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                    Configuration.SchemaName
                }' AND TABLE_NAME = '{Configuration.MetaTableName}')
            BEGIN
                CREATE TABLE {Configuration.FullMetaTableName} (
	                {Configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {Configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    CONSTRAINT PK_{Configuration.MetaTableName} PRIMARY KEY ({Configuration.PersistenceIdColumnName}, {
                    Configuration.SequenceNrColumnName
                })
                );
            END
            ";
        }

        protected override string ByTagSql { get; }
        protected override string AllEventsSql { get;}
        protected override string CreateEventsJournalSql { get; }
        protected override string CreateMetaTableSql { get; }

        protected override DbCommand CreateCommand(DbConnection connection)
        {
            return new SqlCommand { Connection = (SqlConnection) connection };
        }

        /// <summary>
        /// Sets column sizes loaded from db schema, so that constant parameter sizes could be set during parameter generation
        /// </summary>
        internal void SetColumnSizes(JournalColumnSizesInfo columnSizesInfo)
        {
            _columnSizes = columnSizesInfo;
        }

        /// <inheritdoc />
        protected override void PreAddParameterToCommand(DbCommand command, DbParameter param)
        {
            if (!_columnSizes.HasValue)
                return;
            
            // if column sizes are loaded, use them to define constant parameter size values
            switch (param.ParameterName)
            {
                case "@PersistenceId":
                    param.Size = _columnSizes.Value.PersistenceIdColumnSize;
                    break;
                
                case "@Tag":
                    param.Size = _columnSizes.Value.TagsColumnSize;
                    break;
                
                case "@Manifest":
                    param.Size = _columnSizes.Value.ManifestColumnSize;
                    break;
            }
        }
    }
}