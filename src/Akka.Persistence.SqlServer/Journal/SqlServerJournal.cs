using System.Data.Common;
using System.Data.SqlClient;
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
            QueryBuilder = new SqlServerJournalQueryBuilder(Settings.TableName, Settings.SchemaName);
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
        public SqlServerJournal() : base(new SqlServerJournalEngine(Context.System))
        {
        }
    }
}