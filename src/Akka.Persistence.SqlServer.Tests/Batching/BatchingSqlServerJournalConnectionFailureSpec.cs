using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.Sql.TestKit;
using Akka.Persistence.TCK.Journal;
using Akka.TestKit;
using Akka.Util.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerJournalConnectionFailureSpec : SqlJournalConnectionFailureSpec
    {
        private static Config CreateSpecConfig(string connectionString)
        {
            return ConfigurationFactory.ParseString(@"
                akka.persistence {
                    publish-plugin-commands = on
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
                            refresh-interval = 1s
                        }
                    }
                }");
        }

        public BatchingSqlServerJournalConnectionFailureSpec(ITestOutputHelper output)
            : base(CreateSpecConfig(DefaultInvalidConnectionString), output)
        {
        }
    }
}
