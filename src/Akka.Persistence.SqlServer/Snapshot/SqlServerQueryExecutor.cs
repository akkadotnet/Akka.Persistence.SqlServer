//-----------------------------------------------------------------------
// <copyright file="SqlServerQueryExecutor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Data.Common;
using System.Data.SqlClient;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.SqlServer.Snapshot
{
    public class SqlServerQueryExecutor : AbstractQueryExecutor
    {
        public SqlServerQueryExecutor(QueryConfiguration configuration, Akka.Serialization.Serialization serialization) : base(configuration, serialization)
        {
            CreateSnapshotTableSql = $@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{configuration.SchemaName}' AND TABLE_NAME = '{configuration.SnapshotTableName}')
            BEGIN
                CREATE TABLE {configuration.FullSnapshotTableName} (
	                {configuration.PersistenceIdColumnName} NVARCHAR(255) NOT NULL,
	                {configuration.SequenceNrColumnName} BIGINT NOT NULL,
                    {configuration.TimestampColumnName} DATETIME2 NOT NULL,
                    {configuration.ManifestColumnName} NVARCHAR(500) NOT NULL,
	                {configuration.PayloadColumnName} VARBINARY(MAX) NOT NULL,
                    {configuration.SerializerIdColumnName} INTEGER NULL
                    CONSTRAINT PK_{configuration.SnapshotTableName} PRIMARY KEY ({configuration.PersistenceIdColumnName}, {configuration.SequenceNrColumnName})
                );
                CREATE INDEX IX_{configuration.SnapshotTableName}_{configuration.SequenceNrColumnName} ON {configuration.FullSnapshotTableName}({configuration.SequenceNrColumnName});
                CREATE INDEX IX_{configuration.SnapshotTableName}_{configuration.TimestampColumnName} ON {configuration.FullSnapshotTableName}({configuration.TimestampColumnName});
            END
            ";

            InsertSnapshotSql = $@"
            IF (
                SELECT COUNT(*) 
                FROM {configuration.FullSnapshotTableName}
                WHERE {configuration.SequenceNrColumnName} = @SequenceNr 
                AND {configuration.PersistenceIdColumnName} = @PersistenceId) > 0 
            UPDATE {configuration.FullSnapshotTableName} 
            SET 
                {configuration.PersistenceIdColumnName} = @PersistenceId, 
                {configuration.SequenceNrColumnName} = @SequenceNr, 
                {configuration.TimestampColumnName} = @Timestamp, 
                {configuration.ManifestColumnName} = @Manifest, 
                {configuration.PayloadColumnName} = @Payload,
                {configuration.SerializerIdColumnName} = @SerializerId
            WHERE {configuration.SequenceNrColumnName} = @SequenceNr 
            AND {configuration.PersistenceIdColumnName} = @PersistenceId ELSE 
            INSERT INTO {configuration.FullSnapshotTableName} (
                {configuration.PersistenceIdColumnName}, 
                {configuration.SequenceNrColumnName}, 
                {configuration.TimestampColumnName}, 
                {configuration.ManifestColumnName}, 
                {configuration.PayloadColumnName},
                {configuration.SerializerIdColumnName}) 
            VALUES (@PersistenceId, @SequenceNr, @Timestamp, @Manifest, @Payload, @SerializerId);";

            SelectSnapshotSql = $@"
                SELECT TOP 1 {Configuration.PersistenceIdColumnName},
                    {Configuration.SequenceNrColumnName}, 
                    {Configuration.TimestampColumnName}, 
                    {Configuration.ManifestColumnName}, 
                    {Configuration.PayloadColumnName},
                    {Configuration.SerializerIdColumnName}
                FROM {Configuration.FullSnapshotTableName} 
                WHERE {Configuration.PersistenceIdColumnName} = @PersistenceId 
                    AND {Configuration.SequenceNrColumnName} <= @SequenceNr
                    AND {Configuration.TimestampColumnName} <= @Timestamp
                ORDER BY {Configuration.SequenceNrColumnName} DESC";

        }

        protected override DbCommand CreateCommand(DbConnection connection) => new SqlCommand {Connection = (SqlConnection) connection};

        protected override string CreateSnapshotTableSql { get; }
        protected override string InsertSnapshotSql { get; }
        protected override string SelectSnapshotSql { get; }
    }
}