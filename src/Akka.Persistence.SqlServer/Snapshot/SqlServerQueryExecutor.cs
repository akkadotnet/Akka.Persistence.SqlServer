// -----------------------------------------------------------------------
// <copyright file="SqlServerQueryExecutor.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Data.SqlClient;
using Akka.Persistence.Sql.Common.Snapshot;
using Akka.Persistence.SqlServer.Helpers;
using Akka.Util;

namespace Akka.Persistence.SqlServer.Snapshot
{
    public class SqlServerQueryExecutor : AbstractQueryExecutor
    {
        private Option<SnapshotColumnSizesInfo> _columnSizes = Option<SnapshotColumnSizesInfo>.None;
        
        public SqlServerQueryExecutor(QueryConfiguration configuration, Akka.Serialization.Serialization serialization)
            : base(configuration, serialization)
        {
            CreateSnapshotTableSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{
                    configuration.SchemaName
                }' AND TABLE_NAME = '{configuration.SnapshotTableName}')
            BEGIN
                CREATE TABLE {configuration.FullSnapshotTableName} (
	                {configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    {configuration.TimestampColumnName} DATETIME2 NOT NULL,
                    {configuration.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                {configuration.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                    {configuration.SerializerIdColumnName} INTEGER NULL
                    CONSTRAINT PK_{configuration.SnapshotTableName} PRIMARY KEY ({
                    configuration.PersistenceIdColumnName
                }, {configuration.SequenceNrColumnName})
                );
                CREATE INDEX IX_{configuration.SnapshotTableName}_{configuration.SequenceNrColumnName} ON {
                    configuration.FullSnapshotTableName
                }({configuration.SequenceNrColumnName});
                CREATE INDEX IX_{configuration.SnapshotTableName}_{configuration.TimestampColumnName} ON {
                    configuration.FullSnapshotTableName
                }({configuration.TimestampColumnName});
            END
            ";

            InsertSnapshotSql = $@"
            DECLARE @Manifest_sized NVARCHAR(500);
            DECLARE @Payload_sized VARBINARY(MAX);
            DECLARE @PersistenceId_sized NVARCHAR(255);
            SET @Manifest_sized = @Manifest;
            SET @Payload_sized = @Payload;
            SET @PersistenceId_sized = @PersistenceId;
            IF (
                SELECT COUNT(*) 
                FROM {configuration.FullSnapshotTableName}
                WHERE {configuration.SequenceNrColumnName} = @SequenceNr 
                AND {configuration.PersistenceIdColumnName} = @PersistenceId_sized) > 0 
            UPDATE {configuration.FullSnapshotTableName} 
            SET 
                {configuration.PersistenceIdColumnName} = @PersistenceId_sized, 
                {configuration.SequenceNrColumnName} = @SequenceNr, 
                {configuration.TimestampColumnName} = @Timestamp, 
                {configuration.ManifestColumnName} = @Manifest_sized,
                {configuration.PayloadColumnName} = @Payload_sized,
                {configuration.SerializerIdColumnName} = @SerializerId
            WHERE {configuration.SequenceNrColumnName} = @SequenceNr 
            AND {configuration.PersistenceIdColumnName} = @PersistenceId_sized ELSE 
            INSERT INTO {configuration.FullSnapshotTableName} (
                {configuration.PersistenceIdColumnName}, 
                {configuration.SequenceNrColumnName}, 
                {configuration.TimestampColumnName}, 
                {configuration.ManifestColumnName}, 
                {configuration.PayloadColumnName},
                {configuration.SerializerIdColumnName}) 
            VALUES (@PersistenceId_sized, @SequenceNr, @Timestamp, @Manifest_sized, @Payload_sized, @SerializerId);";

            SelectSnapshotSql = $@"
                DECLARE @PersistenceId_sized NVARCHAR(255);
                SET @PersistenceId_sized = @PersistenceId;
                SELECT TOP 1 {Configuration.PersistenceIdColumnName},
                    {Configuration.SequenceNrColumnName}, 
                    {Configuration.TimestampColumnName}, 
                    {Configuration.ManifestColumnName}, 
                    {Configuration.PayloadColumnName},
                    {Configuration.SerializerIdColumnName}
                FROM {Configuration.FullSnapshotTableName} 
                WHERE {Configuration.PersistenceIdColumnName} = @PersistenceId_sized
                    AND {Configuration.SequenceNrColumnName} <= @SequenceNr
                    AND {Configuration.TimestampColumnName} <= @Timestamp
                ORDER BY {Configuration.SequenceNrColumnName} DESC";
        }

        protected override string CreateSnapshotTableSql { get; }
        protected override string InsertSnapshotSql { get; }
        protected override string SelectSnapshotSql { get; }

        protected override DbCommand CreateCommand(DbConnection connection)
        {
            return new SqlCommand {Connection = (SqlConnection) connection};
        }

        /// <summary>
        /// Sets column sizes loaded from db schema, so that constant parameter sizes could be set during parameter generation
        /// </summary>
        internal void SetColumnSizes(SnapshotColumnSizesInfo columnSizesInfo)
        {
            _columnSizes = columnSizesInfo;
        }

        /// <inheritdoc />
        protected override void CustomDbParameterSetup(DbCommand command, DbParameter param)
        {
            if (!_columnSizes.HasValue)
                return;
            
            // if column sizes are loaded, use them to define constant parameter size values
            switch (param.ParameterName)
            {
                case "@PersistenceId":
                    param.Size = _columnSizes.Value.PersistenceIdColumnSize;
                    break;
                
                case "@Manifest":
                    param.Size = _columnSizes.Value.ManifestColumnSize;
                    break;
            }
        }
    }
}