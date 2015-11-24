using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Akka.Persistence.Sql.Common.Journal;
using Akka.Persistence.Sql.Common.Queries;

namespace Akka.Persistence.SqlServer.Journal
{
    internal class SqlServerJournalQueryBuilder : IJournalQueryBuilder
    {
        private readonly string _schemaName;
        private readonly string _tableName;

        private readonly string _selectHighestSequenceNrSql;
        private readonly string _insertMessagesSql;

        public SqlServerJournalQueryBuilder(string tableName, string schemaName)
        {
            _tableName = tableName;
            _schemaName = schemaName;

            _insertMessagesSql = "INSERT INTO {0}.{1} (PersistenceID, SequenceNr, IsDeleted, Manifest, Timestamp, Payload) VALUES (@PersistenceId, @SequenceNr, @IsDeleted, @Manifest, @Timestamp, @Payload)"
                .QuoteSchemaAndTable(_schemaName, _tableName);
            _selectHighestSequenceNrSql = @"SELECT MAX(SequenceNr) FROM {0}.{1} WHERE PersistenceID = @pid".QuoteSchemaAndTable(_schemaName, _tableName);
        }

        public DbCommand SelectEvents(IEnumerable<IHint> hints)
        {
            var sqlCommand = new SqlCommand();

            var sqlized = hints
                .Select(h => HintToSql(h, sqlCommand))
                .Where(x => !string.IsNullOrEmpty(x));

            var where = string.Join(" AND ", sqlized);
            var sql = new StringBuilder("SELECT PersistenceID, SequenceNr, IsDeleted, Manifest, Timestamp, Payload FROM {0}.{1} ".QuoteSchemaAndTable(_schemaName, _tableName));
            if (!string.IsNullOrEmpty(where))
            {
                sql.Append(" WHERE ").Append(where);
            }

            sqlCommand.CommandText = sql.ToString();
            return sqlCommand;
        }

        private string HintToSql(IHint hint, SqlCommand command)
        {
            if (hint is TimestampRange)
            {
                var range = (TimestampRange)hint;
                var sb = new StringBuilder();

                if (range.From.HasValue)
                {
                    sb.Append(" Timestamp >= @TimestampFrom ");
                    command.Parameters.AddWithValue("@TimestampFrom", range.From.Value);
                }
                if (range.From.HasValue && range.To.HasValue) sb.Append("AND");
                if (range.To.HasValue)
                {
                    sb.Append(" Timestamp < @TimestampTo ");
                    command.Parameters.AddWithValue("@TimestampTo", range.To.Value);
                }

                return sb.ToString();
            }
            if (hint is PersistenceIdRange)
            {
                var range = (PersistenceIdRange)hint;
                var sb = new StringBuilder(" PersistenceID IN (");
                var i = 0;
                foreach (var persistenceId in range.PersistenceIds)
                {
                    var paramName = "@Pid" + (i++);
                    sb.Append(paramName).Append(',');
                    command.Parameters.AddWithValue(paramName, persistenceId);
                }
                return range.PersistenceIds.Count == 0
                    ? string.Empty
                    : sb.Remove(sb.Length - 1, 1).Append(')').ToString();
            }
            else if (hint is WithManifest)
            {
                var manifest = (WithManifest)hint;
                command.Parameters.AddWithValue("@Manifest", manifest.Manifest);
                return " manifest = @Manifest";
            }
            else throw new NotSupportedException(string.Format("SqlServer journal doesn't support query with hint [{0}]", hint.GetType()));
        }

        public DbCommand SelectMessages(string persistenceId, long fromSequenceNr, long toSequenceNr, long max)
        {
            var sql = BuildSelectMessagesSql(fromSequenceNr, toSequenceNr, max);
            var command = new SqlCommand(sql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        public DbCommand SelectHighestSequenceNr(string persistenceId)
        {
            var command = new SqlCommand(_selectHighestSequenceNrSql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        public DbCommand InsertBatchMessages(IPersistentRepresentation[] messages)
        {
            var command = new SqlCommand(_insertMessagesSql);
            command.Parameters.Add("@PersistenceId", SqlDbType.NVarChar);
            command.Parameters.Add("@SequenceNr", SqlDbType.BigInt);
            command.Parameters.Add("@IsDeleted", SqlDbType.Bit);
            command.Parameters.Add("@Manifest", SqlDbType.NVarChar);
            command.Parameters.Add("@Timestamp", SqlDbType.DateTime2);
            command.Parameters.Add("@Payload", SqlDbType.VarBinary);

            return command;
        }

        public DbCommand DeleteBatchMessages(string persistenceId, long toSequenceNr, bool permanent)
        {
            var sql = BuildDeleteSql(toSequenceNr, permanent);
            var command = new SqlCommand(sql)
            {
                Parameters = { PersistenceIdToSqlParam(persistenceId) }
            };

            return command;
        }

        private string BuildDeleteSql(long toSequenceNr, bool permanent)
        {
            var sqlBuilder = new StringBuilder();

            if (permanent)
            {
                sqlBuilder.Append("DELETE FROM {0}.{1} ".QuoteSchemaAndTable(_schemaName, _tableName));
            }
            else
            {
                sqlBuilder.Append("UPDATE {0}.{1} SET IsDeleted = 1 ".QuoteSchemaAndTable(_schemaName, _tableName));
            }

            sqlBuilder.Append("WHERE PersistenceId = @pid");

            if (toSequenceNr != long.MaxValue)
            {
                sqlBuilder.Append(" AND SequenceNr <= ").Append(toSequenceNr);
            }

            var sql = sqlBuilder.ToString();
            return sql;
        }

        private string BuildSelectMessagesSql(long fromSequenceNr, long toSequenceNr, long max)
        {
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendFormat(
                @"SELECT {0}
                    PersistenceID,
                    SequenceNr,
                    IsDeleted,
                    Manifest,
                    Timestamp,
                    Payload ", max != long.MaxValue ? "TOP " + max : string.Empty)
                .Append(" FROM {0}.{1} WHERE PersistenceId = @pid".QuoteSchemaAndTable(_schemaName, _tableName));

            // since we guarantee type of fromSequenceNr, toSequenceNr and max
            // we can inline them without risk of SQL injection

            if (fromSequenceNr > 0)
            {
                if (toSequenceNr != long.MaxValue)
                    sqlBuilder.Append(" AND SequenceNr BETWEEN ")
                        .Append(fromSequenceNr)
                        .Append(" AND ")
                        .Append(toSequenceNr);
                else
                    sqlBuilder.Append(" AND SequenceNr >= ").Append(fromSequenceNr);
            }

            if (toSequenceNr != long.MaxValue)
                sqlBuilder.Append(" AND SequenceNr <= ").Append(toSequenceNr);

            var sql = sqlBuilder.ToString();
            return sql;
        }

        private static SqlParameter PersistenceIdToSqlParam(string persistenceId, string paramName = null)
        {
            return new SqlParameter(paramName ?? "@pid", SqlDbType.NVarChar, persistenceId.Length) { Value = persistenceId };
        }
    }
}