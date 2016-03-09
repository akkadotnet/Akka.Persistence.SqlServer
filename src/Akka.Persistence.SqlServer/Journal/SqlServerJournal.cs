using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Journal
{
    /// <summary>
    /// Specialization of the <see cref="JournalDbEngine"/> which uses SQL Server as it's sql backend database.
    /// </summary>
    public class SqlServerJournalEngine : JournalDbEngine
    {
        public SqlServerJournalEngine(ActorSystem system)
            : base(system)
        {
            QueryBuilder = new SqlServerJournalQueryBuilder(Settings.TableName, Settings.SchemaName, "Metadata");
        }

        protected override string JournalConfigPath { get { return SqlServerJournalSettings.ConfigPath; } }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override void CopyParamsToCommand(DbCommand sqlCommand, JournalEntry entry)
        {
            sqlCommand.Parameters["@PersistenceId"].Value = entry.PersistenceId;
            sqlCommand.Parameters["@SequenceNr"].Value = entry.SequenceNr;
            sqlCommand.Parameters["@IsDeleted"].Value = entry.IsDeleted;
            sqlCommand.Parameters["@Manifest"].Value = entry.Manifest;
            sqlCommand.Parameters["@Timestamp"].Value = entry.Timestamp;
            sqlCommand.Parameters["@Payload"].Value = entry.Payload;
        }
    }

    /// <summary>
    /// Persistent journal actor using SQL Server as persistence layer. It processes write requests
    /// one by one in asynchronous manner, while reading results asynchronously.
    /// </summary>
    public class SqlServerJournal : SqlJournal
    {
        public readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);

        private readonly string _updateSequenceNrSql;

        public SqlServerJournal() : base(new SqlServerJournalEngine(Context.System))
        {
            string schemaName = Extension.JournalSettings.SchemaName;
            string tableName = Extension.JournalSettings.MetadataTableName;

            var sb = new StringBuilder();
            sb.Append("IF (SELECT COUNT(*) FROM {0}.{1} WHERE PersistenceId = @PersistenceId) > 0 ".QuoteSchemaAndTable(schemaName, tableName));
            sb.Append(@"UPDATE {0}.{1} SET SequenceNr = @SequenceNr WHERE PersistenceId = @PersistenceId ".QuoteSchemaAndTable(schemaName, tableName));
            sb.Append("ELSE ");
            sb.Append(@"INSERT INTO {0}.{1} (PersistenceId, SequenceNr) VALUES (@PersistenceId, @SequenceNr)".QuoteSchemaAndTable(schemaName, tableName));

            _updateSequenceNrSql = sb.ToString();
        }

        public override Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            return DbEngine.ReadHighestSequenceNrAsync(persistenceId, fromSequenceNr);
        }

        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            long highestSequenceNr = await DbEngine.ReadHighestSequenceNrAsync(persistenceId, 0);
            await base.DeleteMessagesToAsync(persistenceId, toSequenceNr);

            if (highestSequenceNr <= toSequenceNr)
            {
                await UpdateSequenceNr(persistenceId, highestSequenceNr);
            }
        }

        private async Task UpdateSequenceNr(string persistenceId, long toSequenceNr)
        {
            using (DbConnection connection = DbEngine.CreateDbConnection())
            {
                await connection.OpenAsync();
                using (DbCommand sqlCommand = new SqlCommand(_updateSequenceNrSql))
                {
                    sqlCommand.Parameters.Add(new SqlParameter("@PersistenceId", SqlDbType.NVarChar, persistenceId.Length)
                    {
                        Value = persistenceId
                    });
                    sqlCommand.Parameters.Add(new SqlParameter("@SequenceNr", SqlDbType.BigInt)
                    {
                        Value = toSequenceNr
                    });

                    sqlCommand.Connection = connection;
                    sqlCommand.CommandTimeout = (int) Extension.JournalSettings.ConnectionTimeout.TotalMilliseconds;
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}