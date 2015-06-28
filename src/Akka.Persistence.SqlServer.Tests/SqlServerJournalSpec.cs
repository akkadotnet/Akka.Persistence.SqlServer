using System.Configuration;
using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;

namespace Akka.Persistence.SqlServer.Tests
{
    public class SqlServerJournalSpec : JournalSpec
    {
        private static readonly Config SpecConfig;

        static SqlServerJournalSpec()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString.Replace(@"\", "\\");

            var specString = @"
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.sql-server""
                            sql-server {
                                class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = EventJournal
                                schema-name = dbo
                                auto-initialize = on
                                connection-string = ""Data Source=localhost\\SQLEXPRESS;Database=akka_persistence_tests;User Id=akkadotnet;Password=akkadotnet;""
                            }
                        }
                    }";

            SpecConfig = ConfigurationFactory.ParseString(specString);


            //need to make sure db is created before the tests start
            DbUtils.Initialize();
        }

        public SqlServerJournalSpec()
            : base(SpecConfig, "SqlServerJournalSpec")
        {
            DbUtils.Clean();
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}