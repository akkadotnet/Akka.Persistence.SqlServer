using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Akka.Persistence.Sql.Common.Snapshot;

namespace Akka.Persistence.SqlServer.Snapshot
{
    internal class SqlServerSnapshotQueryBuilder : ISnapshotQueryBuilder
    {
        private readonly string _deleteSql;
        private readonly string _insertSql;
        private readonly string _selectSql;

        public SqlServerSnapshotQueryBuilder(SqlServerSnapshotSettings settings)
        {
            var schemaName = settings.SchemaName;
            var tableName = settings.TableName;
            _deleteSql = @"DELETE FROM {0}.{1} WHERE PersistenceId = @PersistenceId ".QuoteSchemaAndTable(schemaName, tableName);
            _insertSql = @"INSERT INTO {0}.{1} (PersistenceId, SequenceNr, Timestamp, Manifest, Snapshot) VALUES (@PersistenceId, @SequenceNr, @Timestamp, @Manifest, @Snapshot)".QuoteSchemaAndTable(schemaName, tableName);
            _selectSql = @"SELECT PersistenceId, SequenceNr, Timestamp, Manifest, Snapshot FROM {0}.{1} WHERE PersistenceId = @PersistenceId".QuoteSchemaAndTable(schemaName, tableName);
        }

        public DbCommand DeleteOne(string persistenceId, long sequenceNr, DateTime timestamp)
        {
            var sqlCommand = new SqlCommand();
            sqlCommand.Parameters.Add(new SqlParameter("@PersistenceId", SqlDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (sequenceNr < long.MaxValue && sequenceNr > 0)
            {
                sb.Append(@"AND SequenceNr = @SequenceNr ");
                sqlCommand.Parameters.Add(new SqlParameter("@SequenceNr", SqlDbType.BigInt) { Value = sequenceNr });
            }

            if (timestamp > DateTime.MinValue && timestamp < DateTime.MaxValue)
            {
                sb.Append(@"AND Timestamp = @Timestamp");
                sqlCommand.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.DateTime2) { Value = timestamp });
            }

            sqlCommand.CommandText = sb.ToString();

            return sqlCommand;
        }

        public DbCommand DeleteMany(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var sqlCommand = new SqlCommand();
            sqlCommand.Parameters.Add(new SqlParameter("@PersistenceId", SqlDbType.NVarChar, persistenceId.Length) { Value = persistenceId });
            var sb = new StringBuilder(_deleteSql);

            if (maxSequenceNr < long.MaxValue && maxSequenceNr > 0)
            {
                sb.Append(@" AND SequenceNr <= @SequenceNr ");
                sqlCommand.Parameters.Add(new SqlParameter("@SequenceNr", SqlDbType.BigInt) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(@" AND Timestamp <= @Timestamp");
                sqlCommand.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.DateTime2) { Value = maxTimestamp });
            }

            sqlCommand.CommandText = sb.ToString();

            return sqlCommand;
        }

        public DbCommand InsertSnapshot(SnapshotEntry entry)
        {
            var sqlCommand = new SqlCommand(_insertSql)
            {
                Parameters =
                {
                    new SqlParameter("@PersistenceId", SqlDbType.NVarChar, entry.PersistenceId.Length) { Value = entry.PersistenceId },
                    new SqlParameter("@SequenceNr", SqlDbType.BigInt) { Value = entry.SequenceNr },
                    new SqlParameter("@Timestamp", SqlDbType.DateTime2) { Value = entry.Timestamp },
                    new SqlParameter("@Manifest", SqlDbType.NVarChar, entry.SnapshotType.Length) { Value = entry.SnapshotType },
                    new SqlParameter("@Snapshot", SqlDbType.VarBinary, entry.Snapshot.Length) { Value = entry.Snapshot }
                }
            };

            return sqlCommand;
        }

        public DbCommand SelectSnapshot(string persistenceId, long maxSequenceNr, DateTime maxTimestamp)
        {
            var sqlCommand = new SqlCommand();
            sqlCommand.Parameters.Add(new SqlParameter("@PersistenceId", SqlDbType.NVarChar, persistenceId.Length) { Value = persistenceId });

            var sb = new StringBuilder(_selectSql);
            if (maxSequenceNr > 0 && maxSequenceNr < long.MaxValue)
            {
                sb.Append(" AND SequenceNr <= @SequenceNr ");
                sqlCommand.Parameters.Add(new SqlParameter("@SequenceNr", SqlDbType.BigInt) { Value = maxSequenceNr });
            }

            if (maxTimestamp > DateTime.MinValue && maxTimestamp < DateTime.MaxValue)
            {
                sb.Append(" AND Timestamp <= @Timestamp ");
                sqlCommand.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.DateTime2) { Value = maxTimestamp });
            }

            sb.Append(" ORDER BY SequenceNr DESC");
            sqlCommand.CommandText = sb.ToString();
            return sqlCommand;
        }
    }
}