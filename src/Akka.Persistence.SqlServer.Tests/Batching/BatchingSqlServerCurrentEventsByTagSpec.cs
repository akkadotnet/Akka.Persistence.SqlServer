// -----------------------------------------------------------------------
// <copyright file="BatchingSqlServerCurrentEventsByTagSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2013 - 2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.SqlServer.Tests.Batching
{
    [Collection("SqlServerSpec")]
    public class BatchingSqlServerCurrentEventsByTagSpec : EventsByTagSpec, IDisposable
    {
        public BatchingSqlServerCurrentEventsByTagSpec(ITestOutputHelper output, SqlServerFixture fixture)
            : base(InitConfig(fixture), nameof(BatchingSqlServerCurrentEventsByTagSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
        }

        private static Config InitConfig(SqlServerFixture fixture)
        {
            DbUtils.Initialize(fixture.ConnectionString);

            var conf = ConfigurationFactory.ParseString($@"
                    akka.loglevel = INFO
                    akka.test.single-expect-default = 10s
                    akka.persistence.journal.plugin = ""akka.persistence.journal.sql-server""
                    akka.persistence.query.journal.sql.refresh-interval = 1s
                    akka.persistence.journal.sql-server {{
                        event-adapters {{
                          color-tagger  = ""Akka.Persistence.TCK.Query.ColorFruitTagger, Akka.Persistence.TCK""
                        }}
                        event-adapter-bindings = {{
                          ""System.String"" = color-tagger
                        }}
                        class = ""Akka.Persistence.SqlServer.Journal.SqlServerJournal, Akka.Persistence.SqlServer""
                        plugin-dispatcher = ""akka.actor.default-dispatcher""
                        table-name = EventJournal
                        schema-name = dbo
                        auto-initialize = on
                        connection-string = ""{DbUtils.ConnectionString}""
                    }}");

            return conf.WithFallback(SqlReadJournal.DefaultConfiguration());
        }

        protected void Dispose(bool disposing)
        {
            DbUtils.Clean();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}