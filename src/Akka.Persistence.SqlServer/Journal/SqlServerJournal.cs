// -----------------------------------------------------------------------
// <copyright file="SqlServerJournal.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Data.SqlClient;
using Akka.Configuration;
using Akka.Persistence.Sql.Common.Journal;

namespace Akka.Persistence.SqlServer.Journal
{
    public class SqlServerJournal : SqlJournal
    {
        public static readonly SqlServerPersistence Extension = SqlServerPersistence.Get(Context.System);

        public SqlServerJournal(Config journalConfig) : base(journalConfig)
        {
            var config = journalConfig.WithFallback(Extension.DefaultJournalConfig);
            QueryExecutor = new SqlServerQueryExecutor(new QueryConfiguration(
                    config.GetString("schema-name"),
                    config.GetString("table-name"),
                    config.GetString("metadata-table-name"),
                    "PersistenceId",
                    "SequenceNr",
                    "Payload",
                    "Manifest",
                    "Timestamp",
                    "IsDeleted",
                    "Tags",
                    "Ordering",
                    "SerializerId",
                    config.GetTimeSpan("connection-timeout"),
                    config.GetString("serializer"),
                    config.GetBoolean("sequential-access")),
                Context.System.Serialization,
                GetTimestampProvider(config.GetString("timestamp-provider")));
        }

        protected override string JournalConfigPath => SqlServerJournalSettings.ConfigPath;
        public override IJournalQueryExecutor QueryExecutor { get; }

        protected override DbConnection CreateDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}