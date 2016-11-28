//-----------------------------------------------------------------------
// <copyright file="BatchingSqlServerJournalSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;
using Akka.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerJournalSpec : JournalSpec
    {
        private static readonly Config SpecConfig;

        static BatchingSqlServerJournalSpec()
        {
            var specString = @"
                    akka.persistence {
                        publish-plugin-commands = on
                        journal {
                            plugin = ""akka.persistence.journal.sql-server""
                            sql-server {
                                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                                plugin-dispatcher = ""akka.actor.default-dispatcher""
                                table-name = EventJournal
                                schema-name = dbo
                                auto-initialize = on
                                connection-string-name = ""TestDb""
                            }
                        }
                    }";

            SpecConfig = ConfigurationFactory.ParseString(specString);


            //need to make sure db is created before the tests start
            DbUtils.Initialize();
        }

        public BatchingSqlServerJournalSpec(ITestOutputHelper output)
            : base(SpecConfig, "BatchingSqlServerJournalSpec", output)
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}