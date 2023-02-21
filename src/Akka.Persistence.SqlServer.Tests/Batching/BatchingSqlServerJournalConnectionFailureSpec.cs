// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerJournalConnectionFailureSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Sql.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerJournalConnectionFailureSpec : SqlJournalConnectionFailureSpec
    {
        public BatchingSqlServerJournalConnectionFailureSpec(ITestOutputHelper output)
            : base(CreateSpecConfig(DefaultInvalidConnectionString), output)
        {
        }

        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString(@"
                akka.persistence {
                    publish-plugin-commands = on
                    akka.persistence.query.journal.sql.refresh-interval = 1s
                    journal {
                        plugin = ""akka.persistence.journal.sql-server""
                        sql-server {
                            class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            table-name = EventJournal
                            schema-name = dbo
                            auto-initialize = on
                            connection-string = """ + connectionString + @"""
                        }
                    }
                }");
        }
    }
}