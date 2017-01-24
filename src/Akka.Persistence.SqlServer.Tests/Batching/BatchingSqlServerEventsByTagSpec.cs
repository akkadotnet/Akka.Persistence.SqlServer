//-----------------------------------------------------------------------
// <copyright file="BatchingSqlServerEventsByTagSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.Sql.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerEventsByTagSpec : EventsByTagSpec
    {
        public static Config Config => ConfigurationFactory.ParseString($@"
            akka.loglevel = INFO
            akka.test.single-expect-default = 10s
            akka.persistence.journal.plugin = ""akka.persistence.journal.sql-server""
            akka.persistence.journal.sql-server {{
                event-adapters {{
                  color-tagger  = ""Akka.Persistence.Sql.TestKit.ColorTagger, Akka.Persistence.Sql.TestKit""
                }}
                event-adapter-bindings = {{
                  ""System.String"" = color-tagger
                }}
                class = ""Akka.Persistence.SqlServer.Journal.BatchingSqlServerJournal, Akka.Persistence.SqlServer""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                table-name = EventJournal
                schema-name = dbo
                auto-initialize = on
                connection-string-name = ""TestDb""
                refresh-interval = 1s
            }}")
            .WithFallback(SqlReadJournal.DefaultConfiguration());

        public BatchingSqlServerEventsByTagSpec(ITestOutputHelper output) : base(Config, output)
        {
            DbUtils.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DbUtils.Clean();
        }
    }
}